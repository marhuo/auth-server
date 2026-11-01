using AuthServer.Application.OAuth.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.OAuth;

[ApiController]
public class JwksController(TokenService tokenService) : ControllerBase
{
    [HttpGet("~/.well-known/jwks.json")]
    public async Task<IActionResult> Jwks()
    {
        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(await tokenService.GetJwksAsync());
    }
}