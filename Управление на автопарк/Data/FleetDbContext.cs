using Microsoft.EntityFrameworkCore;
using Управление_на_автопарк.Models;
namespace Управление_на_автопарк.Data;
public class FleetDbContext(DbContextOptions<FleetDbContext> options) : DbContext(options)
{
 public DbSet<Vehicles> Vehicles => Set<Vehicles>();
 public DbSet<FuelLog> FuelLogs => Set<FuelLog>();
 protected override void OnModelCreating(ModelBuilder modelBuilder)
 {
  base.OnModelCreating(modelBuilder);
  modelBuilder.Entity<Vehicles>().HasIndex(v => v.RegistrationNumber).IsUnique();
  modelBuilder.Entity<Vehicles>().HasMany(v => v.FuelLogs).WithOne(f => f.Vehicle).HasForeignKey(f => f.VehicleID).OnDelete(DeleteBehavior.Restrict);
  modelBuilder.Entity<FuelLog>().ToTable(t => { t.HasCheckConstraint("CK_FuelLogs_Litres", "[Litres] > 0"); t.HasCheckConstraint("CK_FuelLogs_TotalCost", "[TotalCost] > 0"); });
  modelBuilder.Entity<FuelLog>().HasIndex(f => f.VehicleID);
  modelBuilder.Entity<FuelLog>().HasIndex(f => f.FuelDate);
 }
}
