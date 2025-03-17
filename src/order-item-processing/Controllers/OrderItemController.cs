using Dapr;
using Microsoft.AspNetCore.Mvc;
using OrderItemProcessing.Models;
using OrderItemProcessing.Services;
using Serilog;

namespace OrderItemProcessing.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderItemController : ControllerBase
{
    private readonly IOrderItemService _orderItemService;

    public OrderItemController(IOrderItemService workService)
    {
        _orderItemService = workService;
    }

    [HttpPost("/work")]
    [Topic("kafka-pubsub", "newOrderItem")]
    public async Task<IActionResult> ProcessWork(Models.OrderItem work)
    {
        await _orderItemService.ProcessWorkAsync(work, new Dictionary<string, string>());

        return Ok();
    }
}
