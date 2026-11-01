using AuthServer.Web.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Web.Controllers.Admin;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public abstract class AdminControllerBase : ControllerBase;