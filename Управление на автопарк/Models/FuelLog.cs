using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveOps.Models;

public sealed class FuelLog
{
    public int FuelLogID { get; set; }

    [Required]
    [Display(Name = "Vehicle")]
    public int VehicleID { get; set; }

    [Required]
    [Display(Name = "Date")]
    public DateTime FuelDate { get; set; } = DateTime.Today;

    [Range(typeof(decimal), "0.01", "9999.99")]
    [Column(TypeName = "decimal(6,2)")]
    [Display(Name = "Litres")]
    public decimal Litres { get; set; }

    [Range(typeof(decimal), "0.01", "999999.99")]
    [Column(TypeName = "decimal(8,2)")]
    [Display(Name = "Total cost")]
    public decimal TotalCost { get; set; }

    public Vehicle? Vehicle { get; set; }
}
