namespace JobProcessor.Controllers;

using Microsoft.AspNetCore.Mvc;
using JobProcessor.Infrastructure;

[ApiController]
[Route("redis")]
public sealed class RedisController : ControllerBase
{
    private readonly RedisService _redis;

    public RedisController(RedisService redis)
    {
        _redis = redis;
    }
}
