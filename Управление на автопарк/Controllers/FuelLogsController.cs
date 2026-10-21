using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Управление_на_автопарк.Data;
using Управление_на_автопарк.Models;

namespace Управление_на_автопарк.Controllers;

[Authorize]
public class FuelLogsController(FleetDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var logs = await context.FuelLogs.Include(f => f.Vehicle)
            .OrderByDescending(f => f.FuelDate).ToListAsync();
        return View(logs);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var log = await context.FuelLogs.Include(f => f.Vehicle)
            .FirstOrDefaultAsync(f => f.FuelLogID == id);
        return log is null ? NotFound() : View(log);
    }

    public async Task<IActionResult> Create()
    {
        await LoadVehicles();
        return View(new FuelLog { FuelDate = DateTime.Now });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("VehicleID,FuelDate,Litres,TotalCost")] FuelLog log)
    {
        if (ModelState.IsValid)
        {
            context.Add(log);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        await LoadVehicles(log.VehicleID);
        return View(log);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var log = await context.FuelLogs.FindAsync(id);
        if (log is null) return NotFound();
        await LoadVehicles(log.VehicleID);
        return View(log);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("FuelLogID,VehicleID,FuelDate,Litres,TotalCost")] FuelLog log)
    {
        if (id != log.FuelLogID) return NotFound();
        if (!ModelState.IsValid)
        {
            await LoadVehicles(log.VehicleID);
            return View(log);
        }
        try
        {
            context.Update(log);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await context.FuelLogs.AnyAsync(f => f.FuelLogID == id)) return NotFound();
            throw;
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var log = await context.FuelLogs.Include(f => f.Vehicle)
            .FirstOrDefaultAsync(f => f.FuelLogID == id);
        return log is null ? NotFound() : View(log);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var log = await context.FuelLogs.FindAsync(id);
        if (log is not null)
        {
            context.FuelLogs.Remove(log);
            await context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadVehicles(int? selectedId = null)
    {
        ViewData["VehicleID"] = new SelectList(
            await context.Vehicles.OrderBy(v => v.RegistrationNumber).ToListAsync(),
            "VehicleID", "RegistrationNumber", selectedId);
    }
}
