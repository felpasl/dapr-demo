using Dapr;
using Dapr.Common.Logging;
using Microsoft.AspNetCore.Mvc;
using OrderItemProcessing.Models;
using OrderItemProcessing.Services;

namespace OrderItemProcessing.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderItemController : ControllerBase
{
    private readonly IOrderItemService orderItemService;
    private readonly ILogger<OrderItemController> logger;

    public OrderItemController(
        IOrderItemService workService,
        ILogger<OrderItemController> logger
    )
    {
        this.orderItemService = workService;
        this.logger = logger;
    }

    [HttpPost("/work")]
    [Topic("kafka-pubsub", "newOrderItem")]
    public async Task<IActionResult> ProcessWork(Models.OrderItem work)
    {
        using (this.logger.BeginScope(work.ProcessId.ToString(), "OrderItemReceived"))
        {
            this.logger.LogEvent("Received order item for processing", work);
            await this.orderItemService.ProcessWorkAsync(work, new Dictionary<string, string>());
            return Ok();
        }
    }
}
