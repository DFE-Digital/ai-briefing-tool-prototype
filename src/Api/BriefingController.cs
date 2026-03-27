using BriefingTool.Middleware;
using BriefingTool.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BriefingTool.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class BriefingController(ILogger<BriefingController> logger, IBriefingRunner runner) : ControllerBase
    {
        [HttpPost]
        [ApiKeyAuth]
        [AllowAnonymous]
        public async Task<IActionResult> PostAsync([FromForm] BriefingParameters briefingParameters, bool debug)
        {

            var output = await runner.GetBriefing(briefingParameters);
            
            //DebugPrompt = output.debug;

            return Ok(output.output);
        }
    }
}
