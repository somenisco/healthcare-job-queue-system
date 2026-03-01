namespace JobProcessor.Controllers;

using JobProcessor.Contracts;
using JobProcessor.Core;
using JobProcessor.Infrastructure;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("tests")]
public class TestsController : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly RedisService _redis;
    private readonly JobDbContext _dbContext;
    private readonly IIdGenerator _idGenerator;
    private readonly ActivityLogService _activityLogService;
    private readonly TestOrderEventService _testOrderEventService;

    public TestsController(
        RedisService redis,
        JobDbContext dbContext,
        IIdGenerator idGenerator,
        ActivityLogService activityLogService,
        TestOrderEventService testOrderEventService)
    {
        _redis = redis;
        _dbContext = dbContext;
        _activityLogService = activityLogService;
        _idGenerator = idGenerator;
        _testOrderEventService = testOrderEventService;
    }

    [HttpPost("/samples/{sampleId:long}/tests")]
    public async Task<IActionResult> CreateTestOrder(long sampleId, [FromBody] CreateTestOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TestOrderType))
        {
            return BadRequest("TestOrderType is required.");
        }

        var sampleExists = await _dbContext.Samples
            .AnyAsync(s => s.SampleId == sampleId);
        if (!sampleExists)
        {
            return NotFound("Sample not found.");
        }

        var now = DateTime.UtcNow;
        var testOrder = new TestOrder
        {
            TestOrderId = _idGenerator.NextId(),
            SampleId = sampleId,
            TestOrderType = request.TestOrderType,
            Payload = request.Payload,
            Status = TestOrderStatus.Created,
            RetryCount = 0,
            MaxRetries = request.MaxRetries,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.TestOrders.Add(testOrder);
        await _dbContext.SaveChangesAsync();
        await _activityLogService.PublishAsync($"TestOrder [{testOrder.TestOrderId}] Created.");
        await _testOrderEventService.PublishAsync(ToListItemResponse(testOrder));

        if (request.DelaySeconds > 0)
        {
            var executeAt = DateTimeOffset.UtcNow.AddSeconds(request.DelaySeconds).ToUnixTimeSeconds();
            await _redis.Database.SortedSetAddAsync(
                QueueKeys.ScheduledTestOrders,
                testOrder.TestOrderId.ToString(),
                executeAt
            );
            await _activityLogService.PublishAsync($"TestOrder [{testOrder.TestOrderId}] Scheduled.");
            await _testOrderEventService.PublishAsync(ToListItemResponse(testOrder));
        }
        else
        {
            testOrder.Status = TestOrderStatus.Queued;
            testOrder.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            await _redis.Database.ListLeftPushAsync(
                QueueKeys.TestOrders,
                testOrder.TestOrderId.ToString()
            );
            await _activityLogService.PublishAsync($"TestOrder [{testOrder.TestOrderId}] Queued.");
            await _testOrderEventService.PublishAsync(ToListItemResponse(testOrder));
        }

        return CreatedAtAction(nameof(GetTestOrder), new { testOrderId = testOrder.TestOrderId }, ToResponse(testOrder));
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        await Response.WriteAsync(": stream opened\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        await foreach (var message in _testOrderEventService.ListenAsync(cancellationToken))
        {
            var jsonMessage = System.Text.Json.JsonSerializer.Serialize(message);
            await Response.WriteAsync($"data: {jsonMessage}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    [HttpGet("{testOrderId:long}")]
    public async Task<IActionResult> GetTestOrder(long testOrderId)
    {
        var testOrder = await _dbContext.TestOrders.FindAsync(testOrderId);
        if (testOrder == null)
        {
            return NotFound();
        }

        return Ok(ToResponse(testOrder));
    }

    [HttpGet]
    public async Task<IActionResult> GetTestOrders(
        [FromQuery] TestOrderStatus? testOrderStatus = null,
        [FromQuery] int page = DefaultPage,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        if (page < 1)
        {
            return BadRequest("page must be greater than 0.");
        }

        if (pageSize < 1)
        {
            return BadRequest("pageSize must be greater than 0.");
        }

        var effectivePageSize = Math.Min(pageSize, MaxPageSize);

        IQueryable<TestOrder> query = _dbContext.TestOrders;

        if (testOrderStatus.HasValue)
        {
            query = query.Where(j => j.Status == testOrderStatus.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(j => j.UpdatedAt)
            .ThenByDescending(j => j.TestOrderId)
            .Skip((page - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .Select(j => ToListItemResponse(j))
            .ToListAsync();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)effectivePageSize);

        var response = new PagedResponse<TestOrderListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = effectivePageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return Ok(response);
    }

    private static TestOrderResponse ToResponse(TestOrder testOrder)
    {
        return new TestOrderResponse
        {
            TestOrderId = testOrder.TestOrderId.ToString(),
            SampleId = testOrder.SampleId.ToString(),
            TestOrderType = testOrder.TestOrderType,
            Payload = testOrder.Payload,
            Status = testOrder.Status,
            RetryCount = testOrder.RetryCount,
            MaxRetries = testOrder.MaxRetries,
            CreatedAt = testOrder.CreatedAt,
            UpdatedAt = testOrder.UpdatedAt,
            StartedAt = testOrder.StartedAt,
            CompletedAt = testOrder.CompletedAt
        };
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

