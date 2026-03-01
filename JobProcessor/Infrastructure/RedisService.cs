namespace JobProcessor.Infrastructure;

using Microsoft.Extensions.Options;
using StackExchange.Redis;

public class RedisService : IDisposable
{
    private readonly IConnectionMultiplexer _connection;
    public IDatabase Database => _connection.GetDatabase();
    public ISubscriber Subscriber => _connection.GetSubscriber();

    public RedisService(IOptions<RedisOptions> options)
    {
        var config = ConfigurationOptions.Parse(options.Value.ConnectionString);
        config.ConnectTimeout = options.Value.ConnectTimeoutMs;
        config.SyncTimeout = 10000;
        config.AbortOnConnectFail = false;

        _connection = ConnectionMultiplexer.Connect(config);

        Console.WriteLine("[Redis] Connected successfully");
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
