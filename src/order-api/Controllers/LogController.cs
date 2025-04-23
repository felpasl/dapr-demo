using System.Text.Json;
using Dapr;
using Microsoft.AspNetCore.Mvc;

namespace OrderApi.Controllers;

[ApiController]
[Route("[controller]")]
public class LogController : ControllerBase
{
    private readonly ILogger<LogController> _logger;

    public LogController(ILogger<LogController> logger)
    {
        _logger = logger;
    }

    [HttpPost("event")]
    [Topic("kafka-pubsub", "logs")]
    public IActionResult ReceiveLogEvent([FromBody] object logEvent)
    {
        // Log the received event to console
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("===== DAPR LOG EVENT RECEIVED =====");
        Console.WriteLine(
            JsonSerializer.Serialize(logEvent, new JsonSerializerOptions { WriteIndented = true })
        );
        Console.WriteLine("==================================");
        Console.ResetColor();

        return Ok();
    }
}
