using System.ComponentModel.DataAnnotations;

namespace DriveOps.Models;

public sealed class Vehicle
{
    public int VehicleID { get; set; }

    [Required, StringLength(50)]
    public string Make { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Model { get; set; } = string.Empty;

    [Required, StringLength(15)]
    [Display(Name = "Registration number")]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Range(1950, 2100)]
    [Display(Name = "Manufacture year")]
    public int ManufactureYear { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Current mileage")]
    public int CurrentMileage { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = "Active";

    public ICollection<FuelLog> FuelLogs { get; set; } = new List<FuelLog>();
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
}
