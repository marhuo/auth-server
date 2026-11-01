using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ReactDemo.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DemoController : ControllerBase
    {

        [Authorize(Policy = "ApiPolicy")]
        [HttpGet("/hello-api")]
        public object Get()
        {
            return new { message = "Hello api"};
        }

        [Authorize(Roles = "react_demo_manager")]
        [HttpGet("/hello-manager")]
        public object HelloToManager()
        {
            return new { message = "Hello Manager" };
        }

        [Authorize(Roles = "react_demo_worker")]
        [HttpGet("/hello-worker")]
        public object HelloToWorker()
        {
            return new { message = "Hello Worker" };
        }

    }
}
