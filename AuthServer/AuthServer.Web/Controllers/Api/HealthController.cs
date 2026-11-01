using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Api
{
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthServerApiScope")]
    public class HealthController: ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}
