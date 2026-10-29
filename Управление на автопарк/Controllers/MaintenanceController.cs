using DriveOps.Data;
using DriveOps.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DriveOps.Controllers;

[Authorize(Roles = "Admin")]
[AutoValidateAntiforgeryToken]
public sealed class MaintenanceController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var records = await context.MaintenanceRecords.AsNoTracking().Include(record => record.Vehicle)
            .OrderByDescending(record => record.ServiceDate).ToListAsync();
        return View(records);
    }

    public async Task<IActionResult> Create()
    {
        await LoadVehicles();
        return View(new MaintenanceRecord { ServiceDate = DateTime.Today });
    }

    [HttpPost]
    public async Task<IActionResult> Create(MaintenanceRecord record)
    {
        if (!await context.Vehicles.AnyAsync(v => v.VehicleID == record.VehicleID))
            ModelState.AddModelError(nameof(record.VehicleID), "Select a valid vehicle.");
        if (!ModelState.IsValid)
        {
            await LoadVehicles(record.VehicleID);
            return View(record);
        }
        context.MaintenanceRecords.Add(record);
        await context.SaveChangesAsync();
        TempData["Success"] = "Maintenance record added.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var record = await context.MaintenanceRecords.AsNoTracking().Include(item => item.Vehicle)
            .FirstOrDefaultAsync(item => item.MaintenanceRecordID == id);
        return record is null ? NotFound() : View(record);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var record = await context.MaintenanceRecords.FindAsync(id);
        if (record is null) return NotFound();
        await LoadVehicles(record.VehicleID);
        return View(record);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, MaintenanceRecord record)
    {
        if (id != record.MaintenanceRecordID) return NotFound();
        if (!await context.Vehicles.AnyAsync(vehicle => vehicle.VehicleID == record.VehicleID))
            ModelState.AddModelError(nameof(record.VehicleID), "Select a valid vehicle.");
        if (!ModelState.IsValid)
        {
            await LoadVehicles(record.VehicleID);
            return View(record);
        }
        context.Update(record);
        await context.SaveChangesAsync();
        TempData["Success"] = "Maintenance record updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var record = await context.MaintenanceRecords.AsNoTracking().Include(item => item.Vehicle)
            .FirstOrDefaultAsync(item => item.MaintenanceRecordID == id);
        return record is null ? NotFound() : View(record);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var record = await context.MaintenanceRecords.FindAsync(id);
        if (record is not null)
        {
            context.MaintenanceRecords.Remove(record);
            await context.SaveChangesAsync();
            TempData["Success"] = "Maintenance record deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadVehicles(int? selectedId = null)
    {
        var vehicles = await context.Vehicles.AsNoTracking().OrderBy(v => v.RegistrationNumber).ToListAsync();
        ViewData["VehicleID"] = new SelectList(vehicles, "VehicleID", "RegistrationNumber", selectedId);
    }
}
