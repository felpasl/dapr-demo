using Microsoft.AspNetCore.Mvc;
using Dapr;
using Worker.Models;
using Worker.Services;
using Serilog;

namespace Worker.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorkController : ControllerBase
    {
        private readonly IWorkService _workService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        private const string TRACEPARENT = "traceparent";
        private const string TRACESTATE = "tracestate";

        public WorkController(
            IWorkService workService, 
            IHttpContextAccessor httpContextAccessor)
        {
            _workService = workService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("/work")]
        [Topic("kafka-pubsub", "newWork")]
        public async Task<IActionResult> ProcessWork(WorkTodo work)
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

            await _workService.ProcessWorkAsync(work, metadata);
            
            return Ok();
        }
    }
}
