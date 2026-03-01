namespace JobProcessor.Worker.Services;

/// <summary>
/// Configuration options for demo simulation service.
/// Controls whether to enable demo mode, event rate, seeding, and cleanup behavior.
/// </summary>
public class DemoSimulationOptions
{
    /// <summary>
    /// Gets or sets whether demo simulation is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the rate at which test orders are created per second.
    /// Example: 1.5 means 1.5 test orders per second on average.
    /// </summary>
    public double RatePerSecond { get; set; } = 1.5;

    /// <summary>
    /// Gets or sets whether to seed initial patient and sample data on startup.
    /// Seeds a few records that will be reused for creating test orders.
    /// </summary>
    public bool SeedOnStart { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to cleanup (delete) existing test orders on first startup.
    /// Ensures a clean demo environment for recording videos.
    /// </summary>
    public bool CleanupOnStart { get; set; } = true;

    /// <summary>
    /// Gets or sets the base URL of the API.
    /// Used to construct HTTP calls for creating patients, samples, and test orders.
    /// Example: "http://localhost:5000"
    /// </summary>
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Gets or sets the demo scenario type. Currently supports "mixed" for all scenarios in one flow.
    /// </summary>
    public string DemoScenario { get; set; } = "mixed";
}
