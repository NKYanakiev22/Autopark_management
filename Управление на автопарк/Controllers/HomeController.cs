using DriveOps.Models;
using DriveOps.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DriveOps.Controllers;

public sealed class HomeController(ApplicationDbContext context, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager) : Controller
{
    [Authorize]
    public async Task<IActionResult> Index()
    {
        ViewBag.TotalVehicles = await context.Vehicles.CountAsync();
        ViewBag.ActiveVehicles = await context.Vehicles.CountAsync(vehicle => vehicle.Status == "Active");
        ViewBag.MaintenanceCount = await context.MaintenanceRecords.CountAsync(record => record.Status != "Completed");
        ViewBag.PendingBookings = await context.VehicleBookings.CountAsync(booking => booking.Status == "Pending");
        ViewBag.RecentBookings = await context.VehicleBookings.AsNoTracking().Include(booking => booking.Vehicle)
            .OrderByDescending(booking => booking.StartAt).Take(5).ToListAsync();
        return View();
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reports()
    {
        ViewBag.TotalVehicles = await context.Vehicles.CountAsync();
        ViewBag.ActiveVehicles = await context.Vehicles.CountAsync(vehicle => vehicle.Status == "Active");
        ViewBag.MaintenanceCount = await context.MaintenanceRecords.CountAsync();
        ViewBag.FuelSpend = await context.FuelLogs.SumAsync(log => (decimal?)log.TotalCost) ?? 0m;
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Settings()
    {
        var user = await userManager.GetUserAsync(User);
        return user is null ? Challenge() : View(new SettingsViewModel { FullName = User.FindFirstValue(ClaimTypes.GivenName) ?? user.UserName ?? "", Email = user.Email ?? "" });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(SettingsViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var changingPassword = !string.IsNullOrWhiteSpace(model.NewPassword);
        if (changingPassword && string.IsNullOrWhiteSpace(model.CurrentPassword))
            ModelState.AddModelError(nameof(model.CurrentPassword), "Enter your current password to set a new one.");
        if (!changingPassword && !string.IsNullOrEmpty(model.CurrentPassword))
            ModelState.AddModelError(nameof(model.NewPassword), "Enter a new password or clear the current password field.");
        var duplicateEmail = await userManager.FindByEmailAsync(model.Email);
        if (duplicateEmail is not null && duplicateEmail.Id != user.Id)
            ModelState.AddModelError(nameof(model.Email), "That email address is already in use.");
        if (!ModelState.IsValid) return View(model);

        if (changingPassword)
        {
            var passwordResult = await userManager.ChangePasswordAsync(user, model.CurrentPassword!, model.NewPassword!);
            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }
        }
        user.Email = model.Email;
        user.UserName = model.Email;
        user.NormalizedEmail = userManager.NormalizeEmail(model.Email);
        user.NormalizedUserName = userManager.NormalizeName(model.Email);
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }
        var oldName = (await userManager.GetClaimsAsync(user)).FirstOrDefault(claim => claim.Type == ClaimTypes.GivenName);
        if (oldName is not null) await userManager.RemoveClaimAsync(user, oldName);
        await userManager.AddClaimAsync(user, new Claim(ClaimTypes.GivenName, model.FullName));
        await signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Account settings updated.";
        return RedirectToAction(nameof(Settings));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
