using Consumer.Models;
using Consumer.Services;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Consumer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConsumerController : ControllerBase
    {
        private readonly IConsumerService consumeService;
        private readonly IHttpContextAccessor httpContextAccessor;

        private const string TRACEPARENT = "traceparent";
        private const string TRACESTATE = "tracestate";

        public ConsumerController(
            IConsumerService consumeService,
            IHttpContextAccessor httpContextAccessor
        )
        {
            this.consumeService = consumeService;
            this.httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("/processing")]
        [Topic("kafka-pubsub", "newProcess")]
        public async Task<IActionResult> Processing(ProcessData process)
        {
            Dictionary<string, string> metadata = new Dictionary<string, string>();

            // Get the traceparent header from the current request context
            var httpContext = this.httpContextAccessor.HttpContext;
            if (
                httpContext != null
                && httpContext.Request.Headers.TryGetValue(TRACEPARENT, out var parentValue)
            )
            {
                metadata.Add("cloudevent.traceparent", parentValue.ToString());
            }
            if (
                httpContext != null
                && httpContext.Request.Headers.TryGetValue(TRACESTATE, out var stateValue)
            )
            {
                metadata.Add("cloudevent.tracestate", stateValue.ToString());
            }

            await this.consumeService.ProcessNewWorkAsync(process, metadata);

            return Ok();
        }
    }
}
