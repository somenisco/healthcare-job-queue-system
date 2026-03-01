using System;

namespace JobProcessor.Contracts;

public class CreateTestOrderRequest
{
    public string TestOrderType { get; set; } = String.Empty;
    public string? Payload { get; set; }
    public int DelaySeconds { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
}
