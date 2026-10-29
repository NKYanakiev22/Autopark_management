using DriveOps.Data;
using DriveOps.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DriveOps.Controllers;

[Authorize(Roles = "Admin")]
[AutoValidateAntiforgeryToken]
public sealed class FuelLogsController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var entries = await context.FuelLogs.AsNoTracking().Include(f => f.Vehicle)
            .OrderByDescending(f => f.FuelDate).ToListAsync();
        return View(entries);
    }

    public async Task<IActionResult> Details(int id)
    {
        var entry = await context.FuelLogs.AsNoTracking().Include(f => f.Vehicle)
            .FirstOrDefaultAsync(f => f.FuelLogID == id);
        return entry is null ? NotFound() : View(entry);
    }

    public async Task<IActionResult> Create()
    {
        await LoadVehicles();
        return View(new FuelLog { FuelDate = DateTime.Today });
    }

    [HttpPost]
    public async Task<IActionResult> Create(FuelLog entry)
    {
        if (!await context.Vehicles.AnyAsync(v => v.VehicleID == entry.VehicleID))
            ModelState.AddModelError(nameof(entry.VehicleID), "Select a valid vehicle.");
        if (!ModelState.IsValid)
        {
            await LoadVehicles(entry.VehicleID);
            return View(entry);
        }
        context.FuelLogs.Add(entry);
        await context.SaveChangesAsync();
        TempData["Success"] = "Fuel record added.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var entry = await context.FuelLogs.FindAsync(id);
        if (entry is null) return NotFound();
        await LoadVehicles(entry.VehicleID);
        return View(entry);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, FuelLog entry)
    {
        if (id != entry.FuelLogID) return NotFound();
        if (!await context.Vehicles.AnyAsync(vehicle => vehicle.VehicleID == entry.VehicleID))
            ModelState.AddModelError(nameof(entry.VehicleID), "Select a valid vehicle.");
        if (!ModelState.IsValid)
        {
            await LoadVehicles(entry.VehicleID);
            return View(entry);
        }
        context.Update(entry);
        await context.SaveChangesAsync();
        TempData["Success"] = "Fuel record updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var entry = await context.FuelLogs.AsNoTracking().Include(f => f.Vehicle)
            .FirstOrDefaultAsync(f => f.FuelLogID == id);
        return entry is null ? NotFound() : View(entry);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var entry = await context.FuelLogs.FindAsync(id);
        if (entry is not null)
        {
            context.FuelLogs.Remove(entry);
            await context.SaveChangesAsync();
            TempData["Success"] = "Fuel record deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadVehicles(int? selectedId = null)
    {
        var vehicles = await context.Vehicles.AsNoTracking().OrderBy(v => v.RegistrationNumber).ToListAsync();
        ViewData["VehicleID"] = new SelectList(vehicles, "VehicleID", "RegistrationNumber", selectedId);
    }
}
