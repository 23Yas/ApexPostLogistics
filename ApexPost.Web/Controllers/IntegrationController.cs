namespace ApexPost.Web.Controllers;

using ApexPost.Web.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

[AllowAnonymous]
[Route("integration")]
public sealed class IntegrationController : Controller
{
    [RequireApiKey]
    [RequireAllowedIp]
    [EnableCors("PartnerTrackingWidget")]
    [HttpGet("parcel-status")]
    public IActionResult ParcelStatus()
    {
        return Json(new { status = "InTransit", zone = "North", updatedAtUtc = DateTimeOffset.UtcNow });
    }
}
