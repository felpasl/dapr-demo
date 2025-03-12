using Dapr;
using Microsoft.AspNetCore.Mvc;
using Publisher.Models;
using Publisher.Services;
using Serilog;

namespace Publisher.Controllers;

[ApiController]
[Route("[controller]")]
public class ProcessController : ControllerBase
{
    private readonly IProcessService _processService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string TRACEPARENT = "traceparent";
    private const string TRACESTATE = "tracestate";

    public ProcessController(IProcessService processService, IHttpContextAccessor httpContextAccessor)
    {
        _processService = processService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> StartProcess()
    {
        Dictionary<string, string> metadata = new Dictionary<string, string>();
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.Request.Headers.TryGetValue(TRACEPARENT, out var parentValue))
        {
            metadata.Add("cloudevent.traceparent", parentValue.ToString());
        }
        if (httpContext != null && httpContext.Request.Headers.TryGetValue(TRACESTATE, out var stateValue))
        {
            metadata.Add("cloudevent.tracestate", stateValue.ToString());
        }

        var result = await _processService.StartProcessAsync(metadata);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Topic("kafka-pubsub", "processCompleted")]
    public  IActionResult ProcessFinished(ProcessFinished process)
    {
        Log.Information("Process finished: {id}", process.Id);
        return Ok();
    }

}
