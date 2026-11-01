using AuthServer.Web.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Api
{
    [Route("api/[controller]")]
    [Authorize(Policy = AuthorizationPolicies.ExternalApi)]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}
