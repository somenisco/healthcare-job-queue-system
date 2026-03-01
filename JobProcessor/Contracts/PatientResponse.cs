namespace JobProcessor.Contracts;

public sealed class PatientResponse
{
    public string PatientId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
}
