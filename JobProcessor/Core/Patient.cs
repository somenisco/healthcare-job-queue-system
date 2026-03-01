namespace JobProcessor.Core;

public class Patient
{
    public long PatientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }

    public ICollection<Sample>? Samples { get; set; }
}
