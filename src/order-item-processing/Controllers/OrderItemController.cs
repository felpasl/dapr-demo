using Dapr;
using Microsoft.AspNetCore.Mvc;
using OrderItemProcessing.Models;
using OrderItemProcessing.Services;
using Dapr.Common.Logging;

namespace OrderItemProcessing.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderItemController : ControllerBase
{
    private readonly IOrderItemService orderItemService;
    private readonly ILogger<OrderItemController> logger;
    private readonly BusinessEventLogger<OrderItemController> businessLogger;

    public OrderItemController(
        IOrderItemService workService,
        ILogger<OrderItemController> logger,
        BusinessEventLogger<OrderItemController> businessLogger)
    {
        this.orderItemService = workService;
        this.logger = logger;
        this.businessLogger = businessLogger;
    }

    [HttpPost("/work")]
    [Topic("kafka-pubsub", "newOrderItem")]
    public async Task<IActionResult> ProcessWork(Models.OrderItem work)
    {
        this.businessLogger.LogEvent(
            work.Id.ToString(),
            "OrderItemReceived",
            "Received order item for processing",
            work
        );

        await this.orderItemService.ProcessWorkAsync(work, new Dictionary<string, string>());

        return Ok();
    }
}
