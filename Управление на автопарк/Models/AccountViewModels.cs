using System.ComponentModel.DataAnnotations;

namespace DriveOps.Models;

public sealed class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class SettingsViewModel
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [DataType(DataType.Password), Display(Name = "Current password")]
    public string? CurrentPassword { get; set; }
    [StringLength(100, MinimumLength = 8), DataType(DataType.Password), Display(Name = "New password")]
    public string? NewPassword { get; set; }
    [DataType(DataType.Password), Compare(nameof(NewPassword)), Display(Name = "Confirm new password")]
    public string? ConfirmNewPassword { get; set; }
}
