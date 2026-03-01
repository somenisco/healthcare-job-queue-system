using JobProcessor.Infrastructure;
using JobProcessor.Workers;
using JobProcessor.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<JobWorkerOptions>(
    builder.Configuration.GetSection("JobWorker"));

// Configure demo simulation options
builder.Services.Configure<DemoSimulationOptions>(
    builder.Configuration.GetSection("Simulation"));

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IIdGenerator, SnowflakeIdGenerator>();
builder.Services.AddSingleton<ITestOrderProcessor, TestOrderProcessor>();
builder.Services.AddSingleton<ActivityLogService>(provider =>
    new ActivityLogService(provider.GetRequiredService<RedisService>())
);
builder.Services.AddSingleton<TestOrderEventService>(provider =>
    new TestOrderEventService(provider.GetRequiredService<RedisService>())
);
builder.Services.AddHostedService<JobWorker>();

// Register demo simulation service (runs in background if enabled)
builder.Services.AddHostedService<DemoSimulationBackgroundService>();

var host = builder.Build();
await host.ApplyMigrationsIfConfiguredAsync();
host.Run();
