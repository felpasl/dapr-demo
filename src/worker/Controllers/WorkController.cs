using Dapr;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Worker.Models;
using Worker.Services;

namespace Worker.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorkController : ControllerBase
    {
        private readonly IWorkService _workService;

        public WorkController(IWorkService workService)
        {
            _workService = workService;
        }

        [HttpPost("/work")]
        [Topic("kafka-pubsub", "newWork")]
        public async Task<IActionResult> ProcessWork(WorkTodo work)
        {
            await _workService.ProcessWorkAsync(work, new Dictionary<string, string>());

            return Ok();
        }
    }
}
