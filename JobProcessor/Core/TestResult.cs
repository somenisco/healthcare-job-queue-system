namespace JobProcessor.Core;

public class TestResult
{
    public long TestResultId { get; set; }

    public long TestOrderId { get; set; }
    public TestOrder TestOrder { get; set; } = null!;

    public string ResultValue { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
