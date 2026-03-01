namespace JobProcessor.Workers;

using JobProcessor.Contracts;
using JobProcessor.Core;
using JobProcessor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class JobWorker : BackgroundService
{
    private readonly RedisService _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITestOrderProcessor _testOrderProcessor;
    private readonly ILogger<JobWorker> _logger;
    private readonly TimeSpan _pollDelay;
    private readonly int _brpopTimeoutSeconds;
    private readonly TimeSpan _runningTimeout;
    private readonly int _recoveryBatchSize;
    private readonly ActivityLogService _activityLogService;
    private readonly TestOrderEventService _testOrderEventService;

    public JobWorker(
        RedisService redis,
        ActivityLogService activityLogService,
        TestOrderEventService testOrderEventService,
        IServiceScopeFactory scopeFactory,
        ITestOrderProcessor testOrderProcessor,
        ILogger<JobWorker> logger,
        IOptions<JobWorkerOptions>? options = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _activityLogService = activityLogService ?? throw new ArgumentNullException(nameof(activityLogService));
        _testOrderEventService = testOrderEventService ?? throw new ArgumentNullException(nameof(testOrderEventService));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _testOrderProcessor = testOrderProcessor ?? throw new ArgumentNullException(nameof(testOrderProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var opts = options?.Value ?? new JobWorkerOptions();
        _pollDelay = TimeSpan.FromSeconds(Math.Max(1, opts.PollIntervalSeconds));
        _brpopTimeoutSeconds = Math.Max(1, opts.BrpopTimeoutSeconds);
        _runningTimeout = TimeSpan.FromMinutes(Math.Max(1, opts.RunningTimeoutMinutes));
        _recoveryBatchSize = Math.Max(1, opts.RecoveryBatchSize);

        _logger.LogInformation(
            "JobWorker initialized (PollInterval={Poll}s, RunningTimeout={RunningTimeout}m, RecoveryBatchSize={BatchSize})",
            _pollDelay.TotalSeconds,
            _runningTimeout.TotalMinutes,
            _recoveryBatchSize);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobWorker starting");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RequeueOrphanedJobsAsync(stoppingToken);
                    await RecoverTimedOutRunningJobsAsync(stoppingToken);
                    await ProcessScheduledJobsAsync(stoppingToken);

                    var jobId = await DequeueJobIdAsync(stoppingToken);
                    if (jobId is null)
                    {
                        await Task.Delay(_pollDelay, stoppingToken);
                        continue;
                    }

                    await _testOrderProcessor.ProcessAsync(jobId.Value, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in JobWorker loop");
                    await Task.Delay(_pollDelay, stoppingToken);
                }
            }
        }
        finally
        {
            _logger.LogInformation("JobWorker stopping");
        }
    }

    private async Task ProcessScheduledJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var readyJobs = await _redis.Database.SortedSetRangeByScoreAsync(
                QueueKeys.ScheduledTestOrders,
                stop: now
            );

            if (readyJobs is null || readyJobs.Length == 0)
                return;

            foreach (var ready in readyJobs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var id = ready.ToString();
                if (!long.TryParse(id, out var testOrderId))
                {
                    await _redis.Database.SortedSetRemoveAsync(QueueKeys.ScheduledTestOrders, ready);
                    _logger.LogWarning("Skipping invalid scheduled test order id {JobId}", id);
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();

                var updatedAt = DateTime.UtcNow;
                var updatedRows = await db.TestOrders
                    .Where(j => j.TestOrderId == testOrderId && j.Status == TestOrderStatus.Created)
                    .ExecuteUpdateAsync(
                        updates => updates
                            .SetProperty(j => j.Status, TestOrderStatus.Queued)
                            .SetProperty(j => j.UpdatedAt, updatedAt),
                        cancellationToken);

                if (updatedRows == 0)
                {
                    await _redis.Database.SortedSetRemoveAsync(QueueKeys.ScheduledTestOrders, ready);
                    _logger.LogInformation(
                        "Skipping scheduled test order {JobId}; status no longer Created",
                        id);
                    continue;
                }

                await _redis.Database.ListLeftPushAsync(QueueKeys.TestOrders, id);
                await _redis.Database.SortedSetRemoveAsync(QueueKeys.ScheduledTestOrders, ready);
                _logger.LogDebug("Moved scheduled job {JobId} to queue", id);
                await _activityLogService.PublishAsync(
                    $"TestOrder [{testOrderId}] Queued.");

                var queuedOrder = await db.TestOrders.FirstOrDefaultAsync(
                    j => j.TestOrderId == testOrderId,
                    cancellationToken);
                if (queuedOrder is not null)
                {
                    await _testOrderEventService.PublishAsync(ToListItemResponse(queuedOrder));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed processing scheduled jobs");
        }
    }

    private async Task RequeueOrphanedJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();

            var orphanedCutoff = DateTime.UtcNow.Subtract(_runningTimeout);
            var orphanedIds = await db.TestOrders
                .AsNoTracking()
                .Where(j => (j.Status == TestOrderStatus.Queued ||
                j.Status == TestOrderStatus.Created) &&
                j.UpdatedAt < orphanedCutoff)
                .OrderBy(j => j.UpdatedAt)
                .Select(j => j.TestOrderId)
                .Take(_recoveryBatchSize)
                .ToListAsync(cancellationToken);

            if (orphanedIds.Count == 0)
                return;

            foreach (var testOrderId in orphanedIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var isScheduled = await _redis.Database.SortedSetScoreAsync(
                    QueueKeys.ScheduledTestOrders,
                    testOrderId.ToString());
                if (isScheduled.HasValue)
                {
                    continue;
                }

                await _redis.Database.ListLeftPushAsync(QueueKeys.TestOrders, testOrderId.ToString());
                await _activityLogService.PublishAsync(
                    $"TestOrder [{testOrderId}] Requeued.");

                var requeuedOrder = await db.TestOrders.FirstOrDefaultAsync(
                    j => j.TestOrderId == testOrderId,
                    cancellationToken);
                if (requeuedOrder is not null)
                {
                    requeuedOrder.Status = TestOrderStatus.Queued;
                    requeuedOrder.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                    await _testOrderEventService.PublishAsync(ToListItemResponse(requeuedOrder));
                }
            }

            if (orphanedIds.Count > 0)
            {
                _logger.LogInformation(
                    "Requeued {Count} orphaned test orders", orphanedIds.Count);
            }
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            _logger.LogWarning(ex, "Failed requeueing orphaned test orders");
        }
    }

    private async Task RecoverTimedOutRunningJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var timeoutCutoff = DateTime.UtcNow.Subtract(_runningTimeout);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();

            var staleIds = await db.TestOrders
                .AsNoTracking()
                .Where(j => j.Status == TestOrderStatus.Running && j.StartedAt != null && j.StartedAt < timeoutCutoff)
                .OrderBy(j => j.StartedAt)
                .Select(j => j.TestOrderId)
                .Take(_recoveryBatchSize)
                .ToListAsync(cancellationToken);

            if (staleIds.Count == 0)
                return;

            foreach (var testOrderId in staleIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var updatedAt = DateTime.UtcNow;
                var updatedRows = await db.TestOrders
                    .Where(j => j.TestOrderId == testOrderId && j.Status == TestOrderStatus.Running)
                    .ExecuteUpdateAsync(
                        updates => updates
                            .SetProperty(j => j.Status, TestOrderStatus.Queued)
                            .SetProperty(j => j.StartedAt, (DateTime?)null)
                            .SetProperty(j => j.UpdatedAt, updatedAt),
                        cancellationToken);

                if (updatedRows == 0)
                    continue;

                await _redis.Database.ListLeftPushAsync(QueueKeys.TestOrders, testOrderId.ToString());
                _logger.LogWarning(
                    "Recovered stale running order {TestOrderId}; moved back to queue after timeout {TimeoutMinutes}m",
                    testOrderId,
                    _runningTimeout.TotalMinutes);

                await _activityLogService.PublishAsync(
                    $"TestOrder [{testOrderId}] Recovered.");

                var recoveredOrder = await db.TestOrders.FirstOrDefaultAsync(
                    j => j.TestOrderId == testOrderId,
                    cancellationToken);
                if (recoveredOrder is not null)
                {
                    await _testOrderEventService.PublishAsync(ToListItemResponse(recoveredOrder));
                }
            }
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            _logger.LogWarning(ex, "Failed recovering timed-out running jobs");
        }
    }

    private async Task<long?> DequeueJobIdAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _redis.Database.ExecuteAsync("BRPOP", QueueKeys.TestOrders, _brpopTimeoutSeconds);
            if (result.IsNull)
                return null;

            var jobIdToken = result.Resp2Type == StackExchange.Redis.ResultType.Array && result.Length >= 2 ? result[1] : result;
            var jobIdString = jobIdToken.ToString();

            return long.TryParse(jobIdString, out var id) ? id : (long?)null;
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            _logger.LogWarning(ex, "Error while dequeuing job");
            return null;
        }
    }

    private static TestOrderListItemResponse ToListItemResponse(TestOrder testOrder)
    {
        return new TestOrderListItemResponse
        {
            TestOrderId = testOrder.TestOrderId.ToString(),
            SampleId = testOrder.SampleId.ToString(),
            TestOrderType = testOrder.TestOrderType,
            Status = testOrder.Status,
            RetryCount = testOrder.RetryCount,
            MaxRetries = testOrder.MaxRetries,
            CreatedAt = testOrder.CreatedAt,
            UpdatedAt = testOrder.UpdatedAt
        };
    }
}
