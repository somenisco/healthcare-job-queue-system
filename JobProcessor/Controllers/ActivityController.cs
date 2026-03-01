using JobProcessor.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace JobProcessor.Controllers
{
    [Route("activity")]
    [ApiController]
    public class ActivityController : ControllerBase
    {
        private readonly ActivityLogService _activityLogService;

        public ActivityController(ActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }

        [HttpGet("stream")]
        public async Task Stream(CancellationToken cancellationToken)
        {
            Console.WriteLine("[API] Client connected to activity stream");

            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            //Disable buffering
            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

            // Send initial comment to establish SSE connection
            await Response.WriteAsync(": stream opened\n\n");
            await Response.Body.FlushAsync();

            await foreach (var message in _activityLogService.ListenAsync(cancellationToken))
            {
                var jsonMessage = System.Text.Json.JsonSerializer.Serialize(
                    new { message, timestamp = DateTime.UtcNow }
                );
                await Response.WriteAsync($"data: {jsonMessage}\n\n");
                await Response.Body.FlushAsync();
            }
        }
    }
}
