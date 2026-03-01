using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobProcessor.Infrastructure;

public static class RedisServiceExtensions
{
    public static IServiceCollection AddRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RedisOptions>(
            configuration.GetSection("Redis"));

        // singleton threadsafe connection
        services.AddSingleton<RedisService>();

        return services;
    }
}
