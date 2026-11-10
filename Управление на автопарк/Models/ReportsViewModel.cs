namespace DriveOps.Models;

public sealed class ReportsViewModel
{
    public int TotalVehicles { get; init; }
    public int ActiveVehicles { get; init; }
    public int MaintenanceCount { get; init; }
    public decimal FuelSpend { get; init; }
    public List<MonthlyBookingReport> MonthlyBookings { get; init; } = [];
    public List<ServiceReport> PopularServices { get; init; } = [];
}

public sealed record MonthlyBookingReport(string Month, int Count);
public sealed record ServiceReport(string Service, int Count);
