using OrderProcessing.Models;
using OrderProcessing.Services;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace OrderProcessing.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderProcessingController : ControllerBase
    {
        private readonly IOrderProcessingService consumeService;
        private readonly IHttpContextAccessor httpContextAccessor;

        private const string TRACEPARENT = "traceparent";
        private const string TRACESTATE = "tracestate";

        public OrderProcessingController(
            IOrderProcessingService consumeService,
            IHttpContextAccessor httpContextAccessor
        )
        {
            this.consumeService = consumeService;
            this.httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("/order-processing")]
        [Topic("kafka-pubsub", "newOrder")]
        public async Task<IActionResult> Processing(Order process)
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
