using DriveOps.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DriveOps.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Vehicles")
        : RedirectToAction("Login", "Account");

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
