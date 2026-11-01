using AuthServer.Web.Application.OAuth;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.OAuth;

[ApiController]
public class JwksController(SigningKeyService signingKeys) : ControllerBase
{
    [HttpGet("~/.well-known/jwks.json")]
    public async Task<IActionResult> Jwks()
    {
        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(await signingKeys.GetJwksAsync());
    }
}