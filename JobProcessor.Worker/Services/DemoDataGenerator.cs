using JobProcessor.Contracts;

namespace JobProcessor.Worker.Services;

/// <summary>
/// Generates realistic demo data for simulation.
/// Creates random patient names, sample types, and test order payloads.
/// </summary>
public class DemoDataGenerator
{
    private static readonly string[] FirstNames =
    {
        "John", "Jane", "Michael", "Sarah", "Robert", "Emma", "David", "Olivia",
        "James", "Ava", "William", "Isabella", "Richard", "Sophia", "Joseph", "Mia"
    };

    private static readonly string[] LastNames =
    {
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
        "Rodriguez", "Martinez", "Wilson", "Anderson", "Taylor", "Thomas", "Moore", "Jackson"
    };

    private static readonly string[] SampleTypes =
    {
        "Blood", "Urine", "Saliva", "Tissue", "Serum", "Plasma", "Cerebrospinal Fluid",
        "Synovial Fluid", "Peritoneal Fluid", "Pericardial Fluid", "Semen", "Swab"
    };

    private static readonly string[] TestOrderTypes =
    {
        "CBC", "BMP", "CMP", "Glucose", "Lipid Panel", "Liver Panel", "Kidney Panel",
        "Thyroid Panel", "HIV Test", "Pregnancy Test", "Drug Screen", "Blood Culture",
        "Urinalysis", "Chest X-Ray", "ECG"
    };

    private readonly Random _random;

    public DemoDataGenerator(int? seed = null)
    {
        // Use seed for reproducibility if provided
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// Generates a random patient name.
    /// </summary>
    public string GeneratePatientName()
    {
        var firstName = FirstNames[_random.Next(FirstNames.Length)];
        var lastName = LastNames[_random.Next(LastNames.Length)];
        return $"{firstName} {lastName}";
    }

    /// <summary>
    /// Generates a random date of birth (age 18-80).
    /// </summary>
    public DateTime GenerateDateOfBirth()
    {
        var today = DateTime.Today;
        var age = _random.Next(18, 80);
        var daysInRange = _random.Next(0, 365);
        return today.AddYears(-age).AddDays(daysInRange);
    }

    /// <summary>
    /// Generates a random sample type.
    /// </summary>
    public string GenerateSampleType()
    {
        return SampleTypes[_random.Next(SampleTypes.Length)];
    }

    /// <summary>
    /// Generates a random test order type.
    /// </summary>
    public string GenerateTestOrderType()
    {
        return TestOrderTypes[_random.Next(TestOrderTypes.Length)];
    }

    /// <summary>
    /// Generates an optional delay for scheduled test orders (mostly 0, sometimes 10-60 sec).
    /// This creates variety: some immediate, some scheduled.
    /// </summary>
    public int GenerateDelaySeconds()
    {
        // 80% of orders are created immediately, 20% are scheduled with 10-60 sec delay
        if (_random.NextDouble() < 0.8)
            return 0;

        return _random.Next(10, 61);
    }

    /// <summary>
    /// Generates a random max retries value (2-5).
    /// </summary>
    public int GenerateMaxRetries()
    {
        return _random.Next(2, 6);
    }

    /// <summary>
    /// Generates an optional payload (mostly null, sometimes a small JSON string).
    /// </summary>
    public string? GeneratePayload()
    {
        // 70% null, 30% with some payload
        if (_random.NextDouble() < 0.7)
            return null;

        var payloadId = Guid.NewGuid().ToString("N")[..8];
        return $"{{\"ref\": \"{payloadId}\"}}";
    }

    /// <summary>
    /// Creates a CreatePatientRequest with random data.
    /// </summary>
    public CreatePatientRequest CreateRandomPatientRequest()
    {
        return new CreatePatientRequest
        {
            Name = GeneratePatientName(),
            DateOfBirth = GenerateDateOfBirth()
        };
    }

    /// <summary>
    /// Creates a CreateSampleRequest with random data.
    /// </summary>
    public CreateSampleRequest CreateRandomSampleRequest()
    {
        return new CreateSampleRequest
        {
            SampleType = GenerateSampleType()
        };
    }

    /// <summary>
    /// Creates a CreateTestOrderRequest with random data.
    /// Handles variety: some immediate, some scheduled, some with payloads.
    /// </summary>
    public CreateTestOrderRequest CreateRandomTestOrderRequest()
    {
        return new CreateTestOrderRequest
        {
            TestOrderType = GenerateTestOrderType(),
            Payload = GeneratePayload(),
            DelaySeconds = GenerateDelaySeconds(),
            MaxRetries = GenerateMaxRetries()
        };
    }
}
