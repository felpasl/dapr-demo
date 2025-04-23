using Dapr;
using Dapr.Common.Logging;
using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Models;
using OrderProcessing.Services;

namespace OrderProcessing.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderProcessingController : ControllerBase
{
    private readonly IOrderService consumeService;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<OrderProcessingController> logger;

    private const string TRACEPARENT = "traceparent";
    private const string TRACESTATE = "tracestate";

    public OrderProcessingController(
        IOrderService consumeService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OrderProcessingController> logger
    )
    {
        this.consumeService = consumeService;
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    [HttpPost("/order-processing")]
    [Topic("kafka-pubsub", "newOrder")]
    public async Task<IActionResult> Processing(Order order)
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

        using (this.logger.BeginScope(order.Id.ToString(), "OrderProcessing"))
        {
            this.logger.LogEvent("Processing new order", order);
            await this.consumeService.NewOrderAsync(order, metadata);
            return Ok();
        }
    }
}
