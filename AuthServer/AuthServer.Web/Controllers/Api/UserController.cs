
using AuthServer.Web.Controllers.Api.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Api
{
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthServerApiScope")]
    public class UserController(UserManager<IdentityUser<Guid>> userManager) : ControllerBase
    {
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create(CreateUser request)
        {
            var user = new IdentityUser<Guid>
            {
                UserName = request.Username
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            result = await userManager.AddToRolesAsync(user, request.Roles);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok();
        }
    }
}
