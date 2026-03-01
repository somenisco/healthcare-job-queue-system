namespace JobProcessor.Core;

public class TestOrder
{
    public long TestOrderId { get; set; }
    public long SampleId { get; set; }
    public Sample Sample { get; set; } = null!;
    public string TestOrderType { get; set; } = string.Empty;
    public TestOrderStatus Status { get; set; }
    public string? Payload { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TestResult? TestResult { get; set; }
}
