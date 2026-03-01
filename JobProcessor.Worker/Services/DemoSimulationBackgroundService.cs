using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using JobProcessor.Contracts;
using JobProcessor.Infrastructure;

namespace JobProcessor.Worker.Services;

/// <summary>
/// Background service that simulates demo test order creation for visualization purposes.
/// 
/// On startup:
/// - Optionally cleans up existing test orders (first run only, ensures fresh demo)
/// - Creates seed patients and samples via API if configured
/// 
/// Main loop:
/// - Creates test orders at configured rate (1-2 per second)
/// - Calls the actual API endpoints (POST /patients, /samples, /tests)
/// - Worker processes them naturally, failures happen automatically
/// - Activity log messages drive UI refresh via SSE
/// 
/// Result: A realistic demo showing success paths, failures, retries, and edge cases.
/// </summary>
public class DemoSimulationBackgroundService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JobDbContext _dbContext;
    private readonly DemoSimulationOptions _options;
    private readonly ILogger<DemoSimulationBackgroundService> _logger;
    private readonly DemoDataGenerator _dataGenerator;
    private readonly Random _random = new(42);
    private static readonly (string Name, DateTime DateOfBirth)[] SeedPatients =
    {
        ("Demo Alice", new DateTime(1988, 5, 12)),
        ("Demo Bob", new DateTime(1991, 9, 3)),
        ("Demo Carol", new DateTime(1984, 1, 22)),
        ("Demo David", new DateTime(1995, 11, 18))
    };

    private List<String> _seedPatientIds = new();
    private List<String> _seedSampleIds = new();
    private bool _hasCleanedUp = false;

    public DemoSimulationBackgroundService(
        IHttpClientFactory httpClientFactory,
        JobDbContext dbContext,
        IOptions<DemoSimulationOptions> options,
        ILogger<DemoSimulationBackgroundService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
        // Use fixed seed for reproducibility (same demo sequence on every run)
        _dataGenerator = new DemoDataGenerator(seed: 42);
    }

    /// <summary>
    /// Executes the background service.
    /// Initializes demo data and runs the continuous creation loop.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Demo simulation is disabled. Skipping.");
            return;
        }

        _logger.LogInformation("Demo simulation service starting. ApiBaseUrl: {ApiBaseUrl}, RatePerSecond: {Rate}",
            _options.ApiBaseUrl, _options.RatePerSecond);

        try
        {
            // Initialize: cleanup and seed
            await InitializeAsync(stoppingToken);

            // Main loop: create test orders at configured rate
            await RunSimulationLoopAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo simulation service encountered an error");
        }
    }

    /// <summary>
    /// Initializes the demo environment:
    /// - Cleans up test orders (first startup only)
    /// - Seeds patient and sample data
    /// </summary>
    private async Task InitializeAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Initializing demo simulation...");

        // Cleanup: delete test orders on first run only
        if (_options.CleanupOnStart && !_hasCleanedUp)
        {
            await CleanupTestOrdersAsync(stoppingToken);
            _hasCleanedUp = true;
        }

        // Seed: create initial patients and samples
        if (_options.SeedOnStart)
        {
            await SeedDemoDataWithRetryAsync(stoppingToken);
        }

        if (_seedPatientIds.Count == 0 || _seedSampleIds.Count == 0)
        {
            _logger.LogWarning("No seed data available. Demo will have limited content.");
        }

        _logger.LogInformation("Demo initialization complete. Seeds: {PatientCount} patients, {SampleCount} samples",
            _seedPatientIds.Count, _seedSampleIds.Count);
    }

    /// <summary>
    /// Cleans up existing test orders from the database (first startup only).
    /// Keeps patients and samples intact for reuse.
    /// </summary>
    private async Task CleanupTestOrdersAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Cleaning up existing test orders...");
            var testOrdersToDelete = _dbContext.TestOrders.AsQueryable();
            var count = testOrdersToDelete.Count();

            _dbContext.TestOrders.RemoveRange(testOrdersToDelete);
            await _dbContext.SaveChangesAsync(stoppingToken);

            _logger.LogInformation("Deleted {Count} test orders", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during test order cleanup");
            throw;
        }
    }

    /// <summary>
    /// Seeds initial patient and sample data via API calls.
    /// Retries when API is not ready yet (common in container startup).
    /// </summary>
    private async Task SeedDemoDataWithRetryAsync(CancellationToken stoppingToken)
    {
        const int maxAttempts = 20;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await SeedDemoDataAsync(stoppingToken);
            if (_seedSampleIds.Count > 0)
            {
                return;
            }

            if (attempt == maxAttempts)
            {
                _logger.LogError("Seeding failed after {Attempts} attempts. API may be unavailable at {ApiBaseUrl}",
                    maxAttempts, _options.ApiBaseUrl);
                return;
            }

            _logger.LogWarning("Seeding attempt {Attempt}/{MaxAttempts} failed. Retrying in 2s...",
                attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    /// <summary>
    /// Single seeding attempt: creates patients and samples via API.
    /// </summary>
    private async Task SeedDemoDataAsync(CancellationToken stoppingToken)
    {
        _seedPatientIds.Clear();
        _seedSampleIds.Clear();

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_options.ApiBaseUrl);

            _logger.LogInformation("Seeding demo patients and samples...");

            var existingPatientsResponse = await httpClient.GetAsync("/patients", stoppingToken);
            if (!existingPatientsResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to load patients for seeding: {StatusCode}", existingPatientsResponse.StatusCode);
                return;
            }

            var existingPatients = await existingPatientsResponse.Content.ReadFromJsonAsync<List<PatientResponse>>(cancellationToken: stoppingToken)
                ?? new List<PatientResponse>();

            var patientsByName = existingPatients
                .GroupBy(patient => patient.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var seedPatient in SeedPatients)
            {
                if (patientsByName.TryGetValue(seedPatient.Name, out var existingPatient))
                {
                    _seedPatientIds.Add(existingPatient.PatientId.ToString());
                    continue;
                }

                var patientRequest = new CreatePatientRequest
                {
                    Name = seedPatient.Name,
                    DateOfBirth = seedPatient.DateOfBirth
                };

                var response = await httpClient.PostAsJsonAsync("/patients", patientRequest, cancellationToken: stoppingToken);

                if (response.IsSuccessStatusCode)
                {
                    var patientResponse = await response.Content.ReadFromJsonAsync<PatientResponse>(cancellationToken: stoppingToken);
                    if (patientResponse != null)
                    {
                        _seedPatientIds.Add(patientResponse.PatientId.ToString());
                        _logger.LogDebug("Created seed patient: {PatientId}", patientResponse.PatientId);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to create seed patient: {StatusCode}", response.StatusCode);
                }
            }

            // Ensure 3 samples per seed patient (reuse existing first, create only missing)
            foreach (var patientId in _seedPatientIds)
            {
                var existingSamplesResponse = await httpClient.GetAsync($"/patients/{patientId}/samples", stoppingToken);
                if (!existingSamplesResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to load samples for patient {PatientId}: {StatusCode}",
                        patientId, existingSamplesResponse.StatusCode);
                    continue;
                }

                var existingSamples = await existingSamplesResponse.Content.ReadFromJsonAsync<List<SampleResponse>>(cancellationToken: stoppingToken)
                    ?? new List<SampleResponse>();

                foreach (var sample in existingSamples.Take(3))
                {
                    _seedSampleIds.Add(sample.SampleId.ToString());
                }

                var missingSampleCount = Math.Max(0, 3 - existingSamples.Count);
                for (int i = 0; i < missingSampleCount; i++)
                {
                    var sampleRequest = _dataGenerator.CreateRandomSampleRequest();
                    var response = await httpClient.PostAsJsonAsync($"/patients/{patientId}/samples", sampleRequest, cancellationToken: stoppingToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var sampleResponse = await response.Content.ReadFromJsonAsync<SampleResponse>(cancellationToken: stoppingToken);
                        if (sampleResponse != null)
                        {
                            _seedSampleIds.Add(sampleResponse.SampleId.ToString());
                            _logger.LogDebug("Created seed sample: {SampleId}", sampleResponse.SampleId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create seed sample for patient {PatientId}: {StatusCode}",
                            patientId, response.StatusCode);
                    }
                }
            }

            _logger.LogInformation("Seeding complete. Using {PatientCount} patients and {SampleCount} samples",
                _seedPatientIds.Count, _seedSampleIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during demo data seeding");
            // Don't throw; continue with empty seeds
        }
    }

    /// <summary>
    /// Main simulation loop: creates test orders at configured rate.
    /// Uses fixed delays to maintain steady event stream for video capture.
    /// </summary>
    private async Task RunSimulationLoopAsync(CancellationToken stoppingToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(_options.ApiBaseUrl);

        // Calculate interval based on rate (e.g., 1.5 per second = 667ms per order)
        var intervalMs = (int)(1000.0 / _options.RatePerSecond);

        _logger.LogInformation("Starting simulation loop. Interval: {IntervalMs}ms", intervalMs);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Pick a random seed sample
                if (_seedSampleIds.Count > 0)
                {
                    var randomSampleId = _seedSampleIds[_random.Next(_seedSampleIds.Count)];
                    var testOrderRequest = _dataGenerator.CreateRandomTestOrderRequest();

                    try
                    {
                        // Call POST /samples/{sampleId}/tests
                        var response = await httpClient.PostAsJsonAsync(
                            $"/samples/{randomSampleId}/tests",
                            testOrderRequest,
                            cancellationToken: stoppingToken);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogDebug("Created demo test order for sample {SampleId} ({Type})",
                                randomSampleId, testOrderRequest.TestOrderType);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to create test order: {StatusCode} {ReasonPhrase}",
                                response.StatusCode, response.ReasonPhrase);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error creating test order in simulation loop");
                    }
                }

                // Wait for next interval
                await Task.Delay(intervalMs, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Demo simulation stopped");
        }
    }
}
