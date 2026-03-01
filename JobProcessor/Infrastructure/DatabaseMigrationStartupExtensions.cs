using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobProcessor.Infrastructure;

public static class DatabaseMigrationStartupExtensions
{
    public static async Task ApplyMigrationsIfConfiguredAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        var configuration = services.GetRequiredService<IConfiguration>();
        var environment = services.GetRequiredService<IHostEnvironment>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");
        var dbContext = services.GetRequiredService<JobDbContext>();

        var shouldAutoMigrate = configuration.GetValue<bool?>("Database:AutoMigrateOnStartup")
            ?? environment.IsDevelopment();

        if (!shouldAutoMigrate)
        {
            logger.LogInformation("Database auto-migration is disabled.");
            return;
        }

        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database migrations applied successfully.");
    }
}
