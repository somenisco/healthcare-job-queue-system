using JobProcessor.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.
    Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddSingleton<IIdGenerator, SnowflakeIdGenerator>();

builder.Services.AddRedis(builder.Configuration);
builder.Services.AddSingleton<ActivityLogService>(provider =>
    new ActivityLogService(provider.GetRequiredService<RedisService>())
);
builder.Services.AddSingleton<TestOrderEventService>(provider =>
    new TestOrderEventService(provider.GetRequiredService<RedisService>())
);

var app = builder.Build();

await app.ApplyMigrationsIfConfiguredAsync();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

// Skip HTTPS redirection in development
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.Run();
