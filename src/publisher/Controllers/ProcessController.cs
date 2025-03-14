using Dapr;
using Microsoft.AspNetCore.Mvc;
using Publisher.Models;
using Publisher.Services;

namespace Publisher.Controllers;

[ApiController]
[Route("[controller]")]
public class ProcessController : ControllerBase
{
    private readonly ILogger<ProcessController> logger;
    private readonly IProcessService processService;
    private readonly IHttpContextAccessor httpContextAccessor;
    private const string TRACEPARENT = "traceparent";
    private const string TRACESTATE = "tracestate";

    public ProcessController(
        IProcessService processService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProcessController> logger
    )
    {
        this.processService = processService;
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> StartProcess()
    {
        Dictionary<string, string> metadata = new Dictionary<string, string>();
        var httpContext = httpContextAccessor.HttpContext;
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

        var result = await processService.StartProcessAsync(metadata);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Topic("kafka-pubsub", "processCompleted")]
    public IActionResult ProcessFinished(ProcessFinished process)
    {
        logger.LogInformation("Process finished: {@process}", process);
        return Ok();
    }
}
