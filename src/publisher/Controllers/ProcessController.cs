using Microsoft.AspNetCore.Mvc;
using Publisher.Services;

namespace Publisher.Controllers;

[ApiController]
[Route("[controller]")]
public class ProcessController : ControllerBase
{
    private readonly IProcessService _processService;

    public ProcessController(IProcessService processService)
    {
        _processService = processService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> StartProcess()
    {
        var result = await _processService.StartProcessAsync();
        return Ok(result);
    }
}
