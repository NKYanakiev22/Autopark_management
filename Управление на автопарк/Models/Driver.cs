using System.ComponentModel.DataAnnotations;

namespace DriveOps.Models;

public sealed class Driver
{
    public int DriverID { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; set; }

    [Required, StringLength(50)]
    [Display(Name = "License number")]
    public string LicenseNumber { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "License expiry")]
    public DateTime LicenseExpiry { get; set; } = DateTime.Today.AddYears(1);

    [Required, StringLength(30)]
    public string Status { get; set; } = "Available";
}
