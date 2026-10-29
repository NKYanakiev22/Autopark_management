using System.Security.Claims;
using DriveOps.Data;
using DriveOps.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DriveOps.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public sealed class BookingsController(ApplicationDbContext context, UserManager<IdentityUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        IQueryable<VehicleBooking> query = context.VehicleBookings.AsNoTracking().Include(booking => booking.Vehicle);
        if (!User.IsInRole("Admin"))
            query = query.Where(booking => booking.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));

        var bookings = await query.OrderByDescending(booking => booking.StartAt).ToListAsync();
        if (User.IsInRole("Admin"))
        {
            var userIds = bookings.Select(booking => booking.UserId).Distinct().ToList();
            var submitters = new Dictionary<string, (string Name, string Email)>();
            foreach (var userIdForBooking in userIds)
            {
                var user = await userManager.FindByIdAsync(userIdForBooking);
                if (user is null) continue;
                var claims = await userManager.GetClaimsAsync(user);
                submitters[user.Id] = (claims.FirstOrDefault(claim => claim.Type == ClaimTypes.GivenName)?.Value ?? user.UserName ?? "Unknown user", user.Email ?? "");
            }
            ViewBag.Submitters = submitters;
        }
        return View(bookings);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? vehicleId)
    {
        await LoadVehicles(vehicleId);
        var startAt = DateTime.Now;
        startAt = startAt.AddSeconds(-startAt.Second).AddTicks(-(startAt.Ticks % TimeSpan.TicksPerSecond));
        return View(new VehicleBooking
        {
            VehicleID = vehicleId ?? 0,
            StartAt = startAt.AddHours(1),
            EndAt = startAt.AddHours(2)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(VehicleBooking booking)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        booking.StartAt = new DateTime(booking.StartAt.Year, booking.StartAt.Month, booking.StartAt.Day,
            booking.StartAt.Hour, booking.StartAt.Minute, 0, booking.StartAt.Kind);
        booking.EndAt = new DateTime(booking.EndAt.Year, booking.EndAt.Month, booking.EndAt.Day,
            booking.EndAt.Hour, booking.EndAt.Minute, 0, booking.EndAt.Kind);

        // UserId is supplied from the authenticated identity, never from the form.
        ModelState.Remove(nameof(booking.UserId));
        ModelState.Remove(nameof(booking.Vehicle));
        var vehicle = await context.Vehicles.FirstOrDefaultAsync(item =>
            item.VehicleID == booking.VehicleID && item.Status == "Active");

        if (vehicle is null)
            ModelState.AddModelError(nameof(booking.VehicleID), "Choose an active vehicle.");
        if (booking.StartAt <= DateTime.Now)
            ModelState.AddModelError(nameof(booking.StartAt), "Choose a start time in the future.");
        if (booking.EndAt <= booking.StartAt)
            ModelState.AddModelError(nameof(booking.EndAt), "The end must be after the start.");

        if (vehicle is not null && booking.EndAt > booking.StartAt)
        {
            var hasConflict = await context.VehicleBookings.AnyAsync(existing =>
                existing.VehicleID == booking.VehicleID &&
                (existing.Status == "Pending" || existing.Status == "Approved") &&
                existing.StartAt < booking.EndAt && existing.EndAt > booking.StartAt);
            if (hasConflict)
                ModelState.AddModelError(string.Empty, "This vehicle already has a booking during that time.");
        }

        if (!ModelState.IsValid)
        {
            await LoadVehicles(booking.VehicleID);
            return View(booking);
        }

        booking.UserId = userId;
        booking.Status = "Pending";
        booking.CreatedAt = DateTime.UtcNow;
        context.VehicleBookings.Add(booking);
        await context.SaveChangesAsync();
        TempData["Success"] = "Vehicle booking submitted for approval.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var booking = await context.VehicleBookings.FirstOrDefaultAsync(item => item.VehicleBookingID == id);
        if (booking is null) return NotFound();
        if (!User.IsInRole("Admin") && (booking.UserId != userId || booking.Status == "Cancelled"))
            return Forbid();

        booking.Status = "Cancelled";
        await context.SaveChangesAsync();
        TempData["Success"] = "Booking cancelled.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetStatus(int id, string status)
    {
        if (status is not ("Approved" or "Cancelled")) return BadRequest();
        var booking = await context.VehicleBookings.FindAsync(id);
        if (booking is null) return NotFound();
        booking.Status = status;
        await context.SaveChangesAsync();
        TempData["Success"] = $"Booking {status.ToLowerInvariant()}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadVehicles(int? selectedId)
    {
        ViewBag.VehicleID = new SelectList(
            await context.Vehicles.AsNoTracking().Where(vehicle => vehicle.Status == "Active")
                .OrderBy(vehicle => vehicle.RegistrationNumber).ToListAsync(),
            "VehicleID", "RegistrationNumber", selectedId);
    }
}
