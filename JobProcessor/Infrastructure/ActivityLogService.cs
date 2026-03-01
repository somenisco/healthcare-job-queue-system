using System;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using StackExchange.Redis;

namespace JobProcessor.Infrastructure;

public class ActivityLogService
{
    private readonly RedisService _redisService;
    private readonly RedisChannel _channelName = RedisChannel.Literal("activity:log");

    public ActivityLogService(RedisService redisService)
    {
        _redisService = redisService;
    }

    public async Task PublishAsync(string message)
    {
        await _redisService.Subscriber.PublishAsync(_channelName, message);
    }

    public async IAsyncEnumerable<string> ListenAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<string>();

        Action<RedisChannel, RedisValue> handler = (_, value) =>
        {
            if (!value.IsNullOrEmpty)
            {
                channel.Writer.TryWrite(value.ToString());
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
