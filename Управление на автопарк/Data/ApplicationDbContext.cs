using DriveOps.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DriveOps.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<IdentityUser, IdentityRole, string>(options)
{
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<FuelLog> FuelLogs => Set<FuelLog>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Vehicle>().HasIndex(vehicle => vehicle.RegistrationNumber).IsUnique();
        builder.Entity<Vehicle>()
            .HasMany(vehicle => vehicle.FuelLogs)
            .WithOne(log => log.Vehicle)
            .HasForeignKey(log => log.VehicleID)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Vehicle>()
            .HasMany(vehicle => vehicle.MaintenanceRecords)
            .WithOne(record => record.Vehicle)
            .HasForeignKey(record => record.VehicleID)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FuelLog>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_FuelLogs_Litres", "[Litres] > 0");
            table.HasCheckConstraint("CK_FuelLogs_TotalCost", "[TotalCost] > 0");
        });
        builder.Entity<Driver>().HasIndex(driver => driver.Email).IsUnique();
    }
}
