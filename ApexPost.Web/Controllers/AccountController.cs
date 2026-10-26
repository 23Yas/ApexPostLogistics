namespace ApexPost.Web.Controllers;

using System.Security.Claims;
using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public sealed class AccountController : Controller
{
    private readonly IStaffRepository _staffRepository;

    public AccountController(IStaffRepository staffRepository)
    {
        _staffRepository = staffRepository;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var staff = await _staffRepository.GetByEmailAsync(email, cancellationToken);
        if (staff is null || !staff.IsActive || !PasswordHasher.Verify(password, staff.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid staff credentials.");
            return View();
        }

        var claims = new[]
        {
            new Claim("StaffId", staff.Id.ToString()),
            new Claim(ClaimTypes.Role, staff.Role.ToString()),
            new Claim("Role", staff.Role.ToString()),
            new Claim("BranchCode", staff.HomeBranch.Value),
            new Claim(ClaimTypes.Name, staff.Name),
            new Claim(ClaimTypes.Email, staff.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(HomeController.Dashboard), "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
