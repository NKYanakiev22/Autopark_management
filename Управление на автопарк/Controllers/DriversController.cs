using DriveOps.Data;
using DriveOps.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveOps.Controllers;

[Authorize(Roles = "Admin")]
[AutoValidateAntiforgeryToken]
public sealed class DriversController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        var query = context.Drivers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.FullName.Contains(search) || d.Email.Contains(search) || d.LicenseNumber.Contains(search));
        ViewData["Search"] = search;
        return View(await query.OrderBy(d => d.FullName).ToListAsync());
    }

    public IActionResult Create() => View(new Driver());

    [HttpPost]
    public async Task<IActionResult> Create(Driver driver)
    {
        if (await context.Drivers.AnyAsync(d => d.Email == driver.Email))
            ModelState.AddModelError(nameof(driver.Email), "A driver with this email already exists.");
        if (!ModelState.IsValid) return View(driver);
        context.Drivers.Add(driver);
        await context.SaveChangesAsync();
        TempData["Success"] = "Driver added.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var driver = await context.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.DriverID == id);
        return driver is null ? NotFound() : View(driver);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var driver = await context.Drivers.FindAsync(id);
        return driver is null ? NotFound() : View(driver);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Driver driver)
    {
        if (id != driver.DriverID) return NotFound();
        if (await context.Drivers.AnyAsync(d => d.DriverID != id && d.Email == driver.Email))
            ModelState.AddModelError(nameof(driver.Email), "A driver with this email already exists.");
        if (!ModelState.IsValid) return View(driver);
        context.Update(driver);
        await context.SaveChangesAsync();
        TempData["Success"] = "Driver updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var driver = await context.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.DriverID == id);
        return driver is null ? NotFound() : View(driver);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var driver = await context.Drivers.FindAsync(id);
        if (driver is not null)
        {
            context.Drivers.Remove(driver);
            await context.SaveChangesAsync();
            TempData["Success"] = "Driver deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}
