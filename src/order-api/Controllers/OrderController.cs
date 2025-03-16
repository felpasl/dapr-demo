using Dapr;
using Microsoft.AspNetCore.Mvc;
using OrderApi.Models;
using OrderApi.Services;

namespace OrderApi.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly ILogger<OrderController> logger;
    private readonly IOrderService orderService;
    private readonly IHttpContextAccessor httpContextAccessor;
    private const string TRACEPARENT = "traceparent";
    private const string TRACESTATE = "tracestate";

    public OrderController(
        IOrderService processService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OrderController> logger
    )
    {
        this.orderService = processService;
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    [HttpPost("start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> StartProcess([FromBody] Order data)
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

        var result = await orderService.StartProcessAsync(data, metadata);
        return Ok(result);
    }

    [HttpPost("complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Topic("kafka-pubsub", "orderCompleted")]
    public IActionResult ProcessFinished(OrderCompleted process)
    {
        logger.LogInformation("Order finished: {@process}", process);
        return Ok();
    }
}
