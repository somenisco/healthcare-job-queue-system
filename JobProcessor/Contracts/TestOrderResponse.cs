using JobProcessor.Core;

namespace JobProcessor.Contracts;

public sealed class TestOrderResponse
{
    public string TestOrderId { get; init; } = string.Empty;
    public string SampleId { get; init; } = string.Empty;
    public string TestOrderType { get; init; } = string.Empty;
    public string? Payload { get; init; }
    public TestOrderStatus Status { get; init; }
    public int RetryCount { get; init; }
    public int MaxRetries { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
