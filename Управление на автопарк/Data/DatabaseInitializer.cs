using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DriveOps.Models;

namespace DriveOps.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync("Admin"))
            await roles.CreateAsync(new IdentityRole("Admin"));
        if (!await roles.RoleExistsAsync("User"))
            await roles.CreateAsync(new IdentityRole("User"));

        if (configuration.GetValue<bool>("DemoData:Enabled"))
            await SeedDemoDataAsync(db);

        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var admin = await users.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            admin.PasswordHash = users.PasswordHasher.HashPassword(admin, password);
            var result = await users.CreateAsync(admin);
            if (!result.Succeeded)
                throw new InvalidOperationException("Could not create the configured admin account: " +
                    string.Join("; ", result.Errors.Select(error => error.Description)));
        }
        else if (!await users.CheckPasswordAsync(admin, password))
        {
            admin.PasswordHash = users.PasswordHasher.HashPassword(admin, password);
            admin.SecurityStamp = Guid.NewGuid().ToString();
            var result = await users.UpdateAsync(admin);
            if (!result.Succeeded)
                throw new InvalidOperationException("Could not update the configured admin password: " +
                    string.Join("; ", result.Errors.Select(error => error.Description)));
        }

        if (!await users.IsInRoleAsync(admin, "Admin"))
            await users.AddToRoleAsync(admin, "Admin");

        foreach (var user in await users.Users.ToListAsync())
        {
            if (!await users.IsInRoleAsync(user, "Admin") && !await users.IsInRoleAsync(user, "User"))
                await users.AddToRoleAsync(user, "User");
        }
    }

    private static async Task SeedDemoDataAsync(ApplicationDbContext db)
    {
        var vehicleSeeds = new[]
        {
            new Vehicle { Make = "Ford", Model = "Transit", RegistrationNumber = "FLE-102", ManufactureYear = 2022, CurrentMileage = 62340, Status = "Active" },
            new Vehicle { Make = "Mercedes-Benz", Model = "Sprinter", RegistrationNumber = "FLE-108", ManufactureYear = 2021, CurrentMileage = 91200, Status = "Maintenance" },
            new Vehicle { Make = "Ford", Model = "F-150", RegistrationNumber = "FLE-114", ManufactureYear = 2023, CurrentMileage = 42500, Status = "Active" },
            new Vehicle { Make = "Toyota", Model = "Hilux", RegistrationNumber = "FLE-121", ManufactureYear = 2022, CurrentMileage = 56900, Status = "Active" }
        };

        foreach (var vehicle in vehicleSeeds)
        {
            if (!await db.Vehicles.AnyAsync(item => item.RegistrationNumber == vehicle.RegistrationNumber))
                db.Vehicles.Add(vehicle);
        }
        await db.SaveChangesAsync();

        var driverSeeds = new[]
        {
            new Driver { FullName = "John Davis", Email = "john.davis@driveops.local", Phone = "+1 555 0102", LicenseNumber = "DL-102-8841", LicenseExpiry = DateTime.Today.AddMonths(10), Status = "On Road" },
            new Driver { FullName = "Sarah Wilson", Email = "sarah.wilson@driveops.local", Phone = "+1 555 0108", LicenseNumber = "DL-108-2375", LicenseExpiry = DateTime.Today.AddMonths(2), Status = "Available" }
        };

        foreach (var driver in driverSeeds)
        {
            if (!await db.Drivers.AnyAsync(item => item.Email == driver.Email))
                db.Drivers.Add(driver);
        }
        await db.SaveChangesAsync();

        var vehicleIds = await db.Vehicles
            .Where(vehicle => vehicleSeeds.Select(seed => seed.RegistrationNumber).Contains(vehicle.RegistrationNumber))
            .ToDictionaryAsync(vehicle => vehicle.RegistrationNumber, vehicle => vehicle.VehicleID);

        var today = DateTime.Today;
        var fuelSeeds = new[]
        {
            new FuelLog { VehicleID = vehicleIds["FLE-102"], FuelDate = today.AddDays(-2), Litres = 62.4m, TotalCost = 104.83m },
            new FuelLog { VehicleID = vehicleIds["FLE-114"], FuelDate = today.AddDays(-4), Litres = 48.1m, TotalCost = 82.65m },
            new FuelLog { VehicleID = vehicleIds["FLE-121"], FuelDate = today.AddDays(-6), Litres = 54.7m, TotalCost = 91.20m }
        };

        foreach (var log in fuelSeeds)
        {
            if (!await db.FuelLogs.AnyAsync(item => item.VehicleID == log.VehicleID && item.FuelDate == log.FuelDate))
                db.FuelLogs.Add(log);
        }

        var maintenanceSeeds = new[]
        {
            new MaintenanceRecord { VehicleID = vehicleIds["FLE-108"], Title = "Engine diagnostic", Notes = "Dashboard warning inspection", ServiceDate = today.AddDays(-1), Cost = 245m, Status = "In progress" },
            new MaintenanceRecord { VehicleID = vehicleIds["FLE-102"], Title = "Oil and filter service", Notes = "Routine service", ServiceDate = today.AddDays(12), Cost = 189.50m, Status = "Scheduled" }
        };

        foreach (var record in maintenanceSeeds)
        {
            if (!await db.MaintenanceRecords.AnyAsync(item => item.VehicleID == record.VehicleID && item.Title == record.Title && item.ServiceDate == record.ServiceDate))
                db.MaintenanceRecords.Add(record);
        }
        await db.SaveChangesAsync();
    }
}
