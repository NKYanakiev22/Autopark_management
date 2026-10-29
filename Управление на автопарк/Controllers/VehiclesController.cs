using DriveOps.Data;
using DriveOps.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveOps.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public sealed class VehiclesController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        var query = context.Vehicles.AsNoTracking();
        if (!User.IsInRole("Admin"))
            query = query.Where(vehicle => vehicle.Status == "Active");
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.Make.Contains(search) || v.Model.Contains(search) || v.RegistrationNumber.Contains(search));

        ViewData["Search"] = search;
        return View(await query.OrderBy(v => v.RegistrationNumber).ToListAsync());
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View(new Vehicle { ManufactureYear = DateTime.Today.Year });

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Vehicle vehicle)
    {
        if (await context.Vehicles.AnyAsync(v => v.RegistrationNumber == vehicle.RegistrationNumber))
            ModelState.AddModelError(nameof(vehicle.RegistrationNumber), "This registration number is already in use.");

        if (!ModelState.IsValid) return View(vehicle);
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();
        TempData["Success"] = "Vehicle added.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var vehicle = await context.Vehicles.AsNoTracking()
            .Include(v => v.FuelLogs)
            .Include(v => v.MaintenanceRecords)
            .FirstOrDefaultAsync(v => v.VehicleID == id);
        if (vehicle is null || (!User.IsInRole("Admin") && vehicle.Status != "Active")) return NotFound();
        return View(vehicle);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var vehicle = await context.Vehicles.FindAsync(id);
        return vehicle is null ? NotFound() : View(vehicle);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, Vehicle vehicle)
    {
        if (id != vehicle.VehicleID) return NotFound();
        if (await context.Vehicles.AnyAsync(v => v.VehicleID != id && v.RegistrationNumber == vehicle.RegistrationNumber))
            ModelState.AddModelError(nameof(vehicle.RegistrationNumber), "This registration number is already in use.");
        if (!ModelState.IsValid) return View(vehicle);

        context.Update(vehicle);
        await context.SaveChangesAsync();
        TempData["Success"] = "Vehicle updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var vehicle = await context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.VehicleID == id);
        return vehicle is null ? NotFound() : View(vehicle);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var vehicle = await context.Vehicles.FindAsync(id);
        if (vehicle is null) return RedirectToAction(nameof(Index));

        if (await context.FuelLogs.AnyAsync(log => log.VehicleID == id) ||
            await context.MaintenanceRecords.AnyAsync(record => record.VehicleID == id))
        {
            TempData["Error"] = "This vehicle has fuel or maintenance history and cannot be deleted.";
            return RedirectToAction(nameof(Index));
        }

        context.Vehicles.Remove(vehicle);
        await context.SaveChangesAsync();
        TempData["Success"] = "Vehicle deleted.";
        return RedirectToAction(nameof(Index));
    }
}
