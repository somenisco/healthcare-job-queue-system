namespace JobProcessor.Core;

public class Sample
{
    public long SampleId { get; set; }
    public string SampleType { get; set; } = string.Empty;
    public DateTime CollectedAt { get; set; }

    public long PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public ICollection<TestOrder>? TestOrders { get; set; }
}
