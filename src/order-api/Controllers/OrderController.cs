using Dapr;
using Microsoft.AspNetCore.Mvc;
using OrderApi.Models;
using OrderApi.Services;
using Dapr.Common.Logging;

namespace OrderApi.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly ILogger<OrderController> logger;
    private readonly BusinessEventLogger<OrderController> businessLogger;
    private readonly IOrderService orderService;
    private readonly IHttpContextAccessor httpContextAccessor;
    private const string TRACEPARENT = "traceparent";
    private const string TRACESTATE = "tracestate";

    public OrderController(
        IOrderService processService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OrderController> logger,
        BusinessEventLogger<OrderController> businessLogger
    )
    {
        this.orderService = processService;
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
        this.businessLogger = businessLogger;
    }

    [HttpPost("start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> StartProcess([FromBody] Order data)
    {
        Dictionary<string, string> metadata = new Dictionary<string, string>();
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

        var result = await this.orderService.StartProcessAsync(data, metadata);

        this.businessLogger.LogEvent(
            data.Id.ToString(),
            "StartProcess",
            "Starting order process",
            data
        );

        return Ok(result);
    }

    [HttpPost("complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Topic("kafka-pubsub", "orderCompleted")]
    public IActionResult ProcessFinished(OrderCompleted process)
    {
        this.businessLogger.LogEvent(
            process.Id.ToString(),
            "orderCompleted",
            "Order finished",
            process
        );
        return Ok();
    }
}