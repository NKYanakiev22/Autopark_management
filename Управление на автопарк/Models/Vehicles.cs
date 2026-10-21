using System.ComponentModel.DataAnnotations;
namespace Управление_на_автопарк.Models;
public class Vehicles
{
 public int VehicleID { get; set; }
 [Required, StringLength(50)] public string Make { get; set; } = string.Empty;
 [Required, StringLength(50)] public string Model { get; set; } = string.Empty;
 [Required, StringLength(15)] public string RegistrationNumber { get; set; } = string.Empty;
 [Range(2000, 2100)] public int ManufactureYear { get; set; }
 [Range(0, int.MaxValue)] public int CurrentMileage { get; set; }
 public ICollection<FuelLog> FuelLogs { get; set; } = new List<FuelLog>();
}
