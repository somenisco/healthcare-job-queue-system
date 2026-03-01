using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using JobProcessor.Contracts;
using StackExchange.Redis;

namespace JobProcessor.Infrastructure;

public class TestOrderEventService
{
    private readonly RedisService _redisService;
    private readonly RedisChannel _channelName = RedisChannel.Literal(QueueKeys.TestOrderEvents);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public TestOrderEventService(RedisService redisService)
    {
        _redisService = redisService;
    }

    public async Task PublishAsync(TestOrderListItemResponse testOrder)
    {
        var serialized = JsonSerializer.Serialize(testOrder, SerializerOptions);
        await _redisService.Subscriber.PublishAsync(_channelName, serialized);
    }

    public async IAsyncEnumerable<TestOrderListItemResponse> ListenAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<TestOrderListItemResponse>();

        Action<RedisChannel, RedisValue> handler = (_, value) =>
        {
            if (value.IsNullOrEmpty)
            {
                return;
            }

            try
            {
                var testOrder = JsonSerializer.Deserialize<TestOrderListItemResponse>(value.ToString(), SerializerOptions);
                if (testOrder is not null)
                {
                    channel.Writer.TryWrite(testOrder);
                }
            }
            catch
            {
            }
        };

        await _redisService.Subscriber.SubscribeAsync(_channelName, handler);

        try
        {
            while (await channel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (channel.Reader.TryRead(out var message))
                {
                    yield return message;
                }
            }
        }
        finally
        {
            channel.Writer.Complete();
            await _redisService.Subscriber.UnsubscribeAsync(_channelName, handler);
        }
    }
}
