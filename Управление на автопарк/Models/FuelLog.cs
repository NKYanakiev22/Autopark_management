using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Управление_на_автопарк.Models;
public class FuelLog
{
 public int FuelLogID { get; set; }
 [Required] public int VehicleID { get; set; }
 [Required] public DateTime FuelDate { get; set; } = DateTime.Now;
 [Range(typeof(decimal), "0.01", "9999.99"), Column(TypeName = "decimal(6,2)")] public decimal Litres { get; set; }
 [Range(typeof(decimal), "0.01", "999999.99"), Column(TypeName = "decimal(8,2)")] public decimal TotalCost { get; set; }
 public Vehicles? Vehicle { get; set; }
}
