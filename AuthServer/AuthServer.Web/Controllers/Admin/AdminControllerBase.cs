using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Admin;

[ApiController]
[Authorize(Policy = "AuthServerAdmin")]
public abstract class AdminControllerBase : ControllerBase;