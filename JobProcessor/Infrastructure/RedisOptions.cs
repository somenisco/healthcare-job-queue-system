namespace JobProcessor.Infrastructure;

public class RedisOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public int ConnectTimeoutMs { get; set; }
}
