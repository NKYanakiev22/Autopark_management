using DriveOps.Models;
using DriveOps.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
        return View(await BuildReportsAsync());
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportExcel()
    {
        var report = await BuildReportsAsync();
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Fleet report");
        sheet.Cell(1, 1).Value = "Fleet report";
        sheet.Cell(2, 1).Value = "Total vehicles"; sheet.Cell(2, 2).Value = report.TotalVehicles;
        sheet.Cell(3, 1).Value = "Active vehicles"; sheet.Cell(3, 2).Value = report.ActiveVehicles;
        sheet.Cell(4, 1).Value = "Maintenance records"; sheet.Cell(4, 2).Value = report.MaintenanceCount;
        sheet.Cell(5, 1).Value = "Fuel spend"; sheet.Cell(5, 2).Value = (double)report.FuelSpend;
        var row = 7;
        sheet.Cell(row, 1).Value = "Bookings by month"; sheet.Cell(row, 2).Value = "Count";
        foreach (var item in report.MonthlyBookings) { row++; sheet.Cell(row, 1).Value = item.Month; sheet.Cell(row, 2).Value = item.Count; }
        row += 2; sheet.Cell(row, 1).Value = "Popular services"; sheet.Cell(row, 2).Value = "Count";
        foreach (var item in report.PopularServices) { row++; sheet.Cell(row, 1).Value = item.Service; sheet.Cell(row, 2).Value = item.Count; }
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream(); workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "fleet-report.xlsx");
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportPdf()
    {
        var report = await BuildReportsAsync();
        QuestPDF.Settings.License = LicenseType.Community;
        var bytes = Document.Create(container => container.Page(page =>
        {
            page.Margin(36); page.Header().Text("Fleet operations report").FontSize(20).Bold();
            page.Content().Column(column =>
            {
                column.Item().Text($"Vehicles: {report.TotalVehicles} total, {report.ActiveVehicles} active");
                column.Item().Text($"Maintenance records: {report.MaintenanceCount}");
                column.Item().Text($"Fuel spend: {report.FuelSpend:C}");
                column.Item().PaddingTop(12).Text("Bookings by month").Bold();
                foreach (var item in report.MonthlyBookings) column.Item().Text($"{item.Month}: {item.Count}");
                column.Item().PaddingTop(12).Text("Popular services").Bold();
                foreach (var item in report.PopularServices) column.Item().Text($"{item.Service}: {item.Count}");
            });
            page.Footer().AlignRight().Text($"Generated {DateTime.Now:dd.MM.yyyy HH:mm}");
        })).GeneratePdf();
        return File(bytes, "application/pdf", "fleet-report.pdf");
    }

    private async Task<ReportsViewModel> BuildReportsAsync()
    {
        var bookings = await context.VehicleBookings.AsNoTracking().Select(item => item.StartAt).ToListAsync();
        var now = DateTime.UtcNow;
        var monthly = Enumerable.Range(0, 12).Select(offset => new DateTime(now.Year, now.Month, 1).AddMonths(-11 + offset))
            .Select(month => new MonthlyBookingReport(month.ToString("MMM yyyy"), bookings.Count(date => date.Year == month.Year && date.Month == month.Month))).ToList();
        // Aggregate in SQL, then materialize before ordering to stay compatible with SQLite translation.
        var serviceCounts = await context.MaintenanceRecords.AsNoTracking().GroupBy(item => item.Title)
            .Select(group => new { Service = group.Key, Count = group.Count() }).ToListAsync();
        var services = serviceCounts.OrderByDescending(item => item.Count).Take(5)
            .Select(item => new ServiceReport(item.Service, item.Count)).ToList();
        return new ReportsViewModel
        {
            TotalVehicles = await context.Vehicles.CountAsync(),
            ActiveVehicles = await context.Vehicles.CountAsync(vehicle => vehicle.Status == "Active"),
            MaintenanceCount = await context.MaintenanceRecords.CountAsync(),
            FuelSpend = await context.FuelLogs.SumAsync(log => (decimal?)log.TotalCost) ?? 0m,
            MonthlyBookings = monthly, PopularServices = services
        };
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
