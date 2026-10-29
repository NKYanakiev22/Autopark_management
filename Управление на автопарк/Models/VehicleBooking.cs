using System.ComponentModel.DataAnnotations;

namespace DriveOps.Models;

public sealed class VehicleBooking
{
    public int VehicleBookingID { get; set; }

    [Required]
    public int VehicleID { get; set; }
    public Vehicle? Vehicle { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Booking purpose")]
    public string Purpose { get; set; } = string.Empty;

    [Required, DataType(DataType.DateTime)]
    [Display(Name = "Start")]
    public DateTime StartAt { get; set; }

    [Required, DataType(DataType.DateTime)]
    [Display(Name = "End")]
    public DateTime EndAt { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
