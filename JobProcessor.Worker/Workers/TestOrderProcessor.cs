namespace JobProcessor.Workers;

using JobProcessor.Contracts;
using JobProcessor.Core;
using JobProcessor.Infrastructure;
using Microsoft.EntityFrameworkCore;

public sealed class TestOrderProcessor : ITestOrderProcessor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RedisService _redis;
    private readonly IIdGenerator _idGenerator;
    private readonly ActivityLogService _activityLogService;
    private readonly TestOrderEventService _testOrderEventService;
    private readonly ILogger<TestOrderProcessor> _logger;

    public TestOrderProcessor(
        IServiceScopeFactory scopeFactory,
        RedisService redis,
        IIdGenerator idGenerator,
        ActivityLogService activityLogService,
        TestOrderEventService testOrderEventService,
        ILogger<TestOrderProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
        _idGenerator = idGenerator;
        _activityLogService = activityLogService;
        _testOrderEventService = testOrderEventService;
        _logger = logger;
    }

    public async Task ProcessAsync(long testOrderId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();

        var now = DateTime.UtcNow;
        var claimedRows = await db.TestOrders
            .Where(j => j.TestOrderId == testOrderId && j.Status == TestOrderStatus.Queued)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(j => j.Status, TestOrderStatus.Running)
                    .SetProperty(j => j.StartedAt, now)
                    .SetProperty(j => j.UpdatedAt, now),
                cancellationToken);

        if (claimedRows == 0)
        {
            var current = await db.TestOrders
                .AsNoTracking()
                .Where(j => j.TestOrderId == testOrderId)
                .Select(j => new { j.Status })
                .FirstOrDefaultAsync(cancellationToken);

            if (current is null)
            {
                _logger.LogWarning("Dequeued test order id {TestOrderId} not found in database", testOrderId);
            }
            else
            {
                _logger.LogInformation(
                    "Skipping duplicate dequeue for test order {TestOrderId}; current status is {Status}",
                    testOrderId,
                    current.Status);
            }
            return;
        }

        var testOrder = await db.TestOrders.FirstAsync(
            j => j.TestOrderId == testOrderId,
            cancellationToken);

        try
        {
            _logger.LogInformation("Running test order {TestOrderId}", testOrderId);
            await _activityLogService.PublishAsync($"TestOrder [{testOrder.TestOrderId}] Started.");
            await _testOrderEventService.PublishAsync(ToListItemResponse(testOrder));

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            if (Random.Shared.Next(0, 5) == 0)
                throw new TestOrderFailureException("Simulated failure");

            var hasResult = await db.TestResults
                .AsNoTracking()
                .AnyAsync(r => r.TestOrderId == testOrder.TestOrderId, cancellationToken);

            if (!hasResult)
            {
                var result = new TestResult
                {
                    TestResultId = _idGenerator.NextId(),
                    TestOrderId = testOrder.TestOrderId,
                    ResultValue = GenerateFakeValue(testOrder.TestOrderType),
                    Unit = GetUnit(testOrder.TestOrderType),
                    CreatedAt = DateTime.UtcNow
                };

                db.TestResults.Add(result);
            }
            else
            {
                _logger.LogInformation("Test result already exists for order {TestOrderId}; skipping result insert", testOrderId);
            }

            var completedAt = DateTime.UtcNow;
            testOrder.Status = TestOrderStatus.Success;
            testOrder.CompletedAt = completedAt;
            testOrder.UpdatedAt = completedAt;
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Completed test order {TestOrderId}", testOrderId);
            await _activityLogService.PublishAsync($"TestOrder [{testOrder.TestOrderId}] Completed.");
            await _testOrderEventService.PublishAsync(ToListItemResponse(testOrder));
        }
        catch (TestOrderFailureException ex)
        {
            _logger.LogInformation(ex, "Test order {TestOrderId} failed", testOrderId);
            await HandleFailureAsync(db, testOrder, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Processing of test order {TestOrderId} cancelled", testOrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while processing test order {TestOrderId}", testOrderId);
            await HandleFailureAsync(db, testOrder, cancellationToken);
        }
    }

    private async Task HandleFailureAsync(JobDbContext db, TestOrder testOrder, CancellationToken cancellationToken)
    {
        testOrder.RetryCount += 1;
        testOrder.UpdatedAt = DateTime.UtcNow;

        if (testOrder.RetryCount < testOrder.MaxRetries)
        {
            testOrder.Status = TestOrderStatus.Queued;
            await db.SaveChangesAsync(cancellationToken);

            var backoffSeconds = Math.Pow(2, testOrder.RetryCount);
            var retryTime = DateTimeOffset.UtcNow.AddSeconds(backoffSeconds).ToUnixTimeSeconds();

            await _redis.Database.SortedSetAddAsync(
                QueueKeys.ScheduledTestOrders,
                testOrder.TestOrderId.ToString(),
                retryTime);

            _logger.LogInformation(
                "Test order {TestOrderId} scheduled for retry in {Backoff}s (attempt {Attempt})",
                testOrder.TestOrderId,
                backoffSeconds,
                testOrder.RetryCount);
            await _activityLogService.PublishAsync($"TestOrder [{testOrder.TestOrderId}] Failed. Scheduled for retry.");
            await _testOrderEventService.PublishAsync(ToListItemResponse(testOrder));
            return;
        }

        var completedAt = DateTime.UtcNow;
        testOrder.Status = TestOrderStatus.Dead;
        testOrder.CompletedAt = completedAt;
        testOrder.UpdatedAt = completedAt;
        await db.SaveChangesAsync(cancellationToken);
        _logger.LogWarning("Test order {TestOrderId} reached max retries and marked as dead", testOrder.TestOrderId);
        await _activityLogService.PublishAsync($"TestOrder [{testOrder.TestOrderId}] Dead.");
        await _testOrderEventService.PublishAsync(ToListItemResponse(testOrder));
    }

    private static string GenerateFakeValue(string testType) =>
        testType switch
        {
            "CBC" => Random.Shared.Next(10, 16).ToString(),
            "LFT" => Random.Shared.Next(20, 60).ToString(),
            "TSH" => (Random.Shared.NextDouble() * 4 + 0.5).ToString("F2"),
            "GLU" => Random.Shared.Next(70, 110).ToString(),
            "TC" => Random.Shared.Next(150, 250).ToString(),
            "WBC" => Random.Shared.Next(4, 11).ToString(),
            "RBC" => (Random.Shared.NextDouble() * 4 + 4).ToString("F2"),
            "PLT" => Random.Shared.Next(150, 450).ToString(),
            _ => Random.Shared.Next(1, 100).ToString()
        };

    private static string GetUnit(string testType) =>
        testType switch
        {
            "CBC" => "g/dL",
            "WBC" => "10^3/uL",
            "RBC" => "10^6/uL",
            "PLT" => "10^3/uL",
            "GLU" => "mg/dL",
            "TC" => "mg/dL",
            "TSH" => "uIU/mL",
            "LFT" => "U/L",
            _ => "units"
        };

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

internal sealed class TestOrderFailureException : Exception
{
    public TestOrderFailureException(string? message) : base(message)
    {
    }
}
