using Microsoft.AspNetCore.Mvc;
using Dapr;
using Consumer.Models;
using Consumer.Services;
using Serilog;

namespace Consumer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConsumerController : ControllerBase
    {
        private readonly IConsumerService _processService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        private const string TRACEPARENT = "traceparent";
        private const string TRACESTATE = "tracestate";

        public ConsumerController(
            IConsumerService processService, 
            IHttpContextAccessor httpContextAccessor)
        {
            _processService = processService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("/processing")]
        [Topic("kafka-pubsub", "newProcess")]
        public async Task<IActionResult> Processing(ProcessData process)
        {
            Dictionary<string, string> metadata = new Dictionary<string, string>();

            // Get the traceparent header from the current request context
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null && httpContext.Request.Headers.TryGetValue(TRACEPARENT, out var parentValue))
            {
                metadata.Add("cloudevent.traceparent", parentValue.ToString());
            }
            if (httpContext != null && httpContext.Request.Headers.TryGetValue(TRACESTATE, out var stateValue))
            {
                metadata.Add("cloudevent.tracestate", stateValue.ToString());
            }


            await _processService.ProcessNewWorkAsync(process, metadata);
            
            return Ok();
        }
    }
}
