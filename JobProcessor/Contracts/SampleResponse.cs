namespace JobProcessor.Contracts;

public sealed class SampleResponse
{
    public string SampleId { get; init; } = string.Empty;
    public string SampleType { get; init; } = string.Empty;
    public DateTime CollectedAt { get; init; }
    public string PatientId { get; init; } = string.Empty;
}
