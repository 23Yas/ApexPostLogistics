namespace ApexPost.Web.Controllers;

using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public sealed class StaffController : Controller
{
    private readonly StaffService _staffService;
    private readonly IBranchRepository _branchRepository;

    public StaffController(StaffService staffService, IBranchRepository branchRepository)
    {
        _staffService = staffService;
        _branchRepository = branchRepository;
    }

    [Authorize(Policy = "AdministratorOnly")]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var staff = await _staffService.GetAllAsync(cancellationToken);
        return View(staff.OrderBy(item => item.Name).ToList());
    }

    [Authorize(Policy = "AdministratorOnly")]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var branches = await _branchRepository.GetAllAsync(cancellationToken);
        ViewBag.Branches = branches.OrderBy(item => item.Name).ToList();
        ViewBag.Roles = Enum.GetValues<RoleType>().Select(x => x.ToString()).ToList();
        return View();
    }

    [Authorize(Policy = "AdministratorOnly")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string email, RoleType role, string homeBranch, bool isActive, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, "Name and email are required.");
            return await ReloadCreateView(cancellationToken);
        }

        var branchExists = (await _branchRepository.GetAllAsync(cancellationToken)).Any(item => item.Code == new BranchCode(homeBranch));
        if (!branchExists)
        {
            ModelState.AddModelError(string.Empty, "The selected home branch does not exist.");
            return await ReloadCreateView(cancellationToken);
        }

        var staff = new Staff
        {
            Name = name,
            Email = email,
            Role = role,
            HomeBranch = new BranchCode(homeBranch),
            IsActive = isActive
        };

        try
        {
            await _staffService.CreateAsync(staff, cancellationToken);
            TempData["SuccessMessage"] = $"Staff member {staff.Name} created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await ReloadCreateView(cancellationToken);
        }
    }

    [Authorize(Policy = "AdministratorOnly")]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var staff = await _staffService.GetByIdAsync(id, cancellationToken);
        if (staff is null)
        {
            return NotFound();
        }

        var branches = await _branchRepository.GetAllAsync(cancellationToken);
        ViewBag.Branches = branches.OrderBy(item => item.Name).ToList();
        ViewBag.Roles = Enum.GetValues<RoleType>().Select(x => x.ToString()).ToList();
        return View(staff);
    }

    [Authorize(Policy = "AdministratorOnly")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string name, string email, RoleType role, string homeBranch, bool isActive, CancellationToken cancellationToken)
    {
        var existing = await _staffService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var staff = new Staff
        {
            Id = id,
            Name = name,
            Email = email,
            Role = role,
            HomeBranch = new BranchCode(homeBranch),
            IsActive = isActive
        };

        try
        {
            var updated = await _staffService.UpdateAsync(staff, cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = $"Staff member {updated.Name} updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await ReloadEditView(id, cancellationToken);
        }
    }

    [Authorize(Policy = "AdministratorOnly")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _staffService.DeactivateAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Staff member deactivated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReloadCreateView(CancellationToken cancellationToken)
    {
        var branches = await _branchRepository.GetAllAsync(cancellationToken);
        ViewBag.Branches = branches.OrderBy(item => item.Name).ToList();
        ViewBag.Roles = Enum.GetValues<RoleType>().Select(x => x.ToString()).ToList();
        return View("Create");
    }

    private async Task<IActionResult> ReloadEditView(Guid id, CancellationToken cancellationToken)
    {
        var staff = await _staffService.GetByIdAsync(id, cancellationToken);
        if (staff is null)
        {
            return NotFound();
        }

        var branches = await _branchRepository.GetAllAsync(cancellationToken);
        ViewBag.Branches = branches.OrderBy(item => item.Name).ToList();
        ViewBag.Roles = Enum.GetValues<RoleType>().Select(x => x.ToString()).ToList();
        return View("Edit", staff);
    }
}
