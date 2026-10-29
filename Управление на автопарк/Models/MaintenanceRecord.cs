using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveOps.Models;

public sealed class MaintenanceRecord
{
    public int MaintenanceRecordID { get; set; }

    [Required]
    [Display(Name = "Vehicle")]
    public int VehicleID { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Service / issue")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Service date")]
    public DateTime ServiceDate { get; set; } = DateTime.Today;

    [Range(typeof(decimal), "0", "999999.99")]
    [Column(TypeName = "decimal(8,2)")]
    public decimal Cost { get; set; }

    [Required, StringLength(30)]
    public string Status { get; set; } = "Scheduled";

    public Vehicle? Vehicle { get; set; }
}
