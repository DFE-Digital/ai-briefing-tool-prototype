using BriefingTool.Middleware;
using BriefingTool.Models;
using BriefingTool.Runners.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BriefingTool.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class BriefingController(ILogger<BriefingController> logger, IServiceProvider serviceProvider) : ControllerBase
    {
        [HttpPost]
        [ApiKeyAuth]
        [AllowAnonymous]
        public async Task<IActionResult> PostAsync([FromForm] BriefingParameters briefingParameters, bool debug)
        {
            var service = serviceProvider.GetRequiredKeyedService<IBriefingRunner>(briefingParameters.RunnerServiceType);
            var output = await service.GetBriefing(briefingParameters);

            logger.LogInformation("Briefing output: {Output}", output);
            return Ok(output.Output);
        }
    }
}
