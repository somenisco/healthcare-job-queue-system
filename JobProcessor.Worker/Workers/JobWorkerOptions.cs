using System;

namespace JobProcessor.Workers;

public sealed class JobWorkerOptions
{
    public int PollIntervalSeconds { get; set; } = 5;
    public int BrpopTimeoutSeconds { get; set; } = 5;
    public int RunningTimeoutMinutes { get; set; } = 5;
    public int RecoveryBatchSize { get; set; } = 100;
}
