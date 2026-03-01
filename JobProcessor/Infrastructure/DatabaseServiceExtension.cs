using JobProcessor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobProcessor.Infrastructure;

public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetSection("Database")["ConnectionString"];

        services.AddDbContext<JobDbContext>(options =>
        {
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString)
            );
        });

        return services;
    }
}