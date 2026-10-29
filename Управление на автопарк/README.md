# DriveOps

DriveOps stores users and fleet records in the SQLite database `driveops.db`. EF Core applies the checked-in migrations on application startup.

## First run and administrator account

Development startup seeds this administrator account:

```text
Email:    admin@driveops.local
Password: admin1234
```

The development seed is defined in `appsettings.Development.json`. For production, override the bootstrap values before the first start with environment variables:

Development also loads a small sample fleet, two drivers, fuel logs, and maintenance records. The seed checks for existing IDs/record keys and does not duplicate them on restart.

```powershell
$env:BootstrapAdmin__Email = "admin@your-company.com"
$env:BootstrapAdmin__Password = "Choose-a-unique-password-123!"
dotnet run
```

The configured account is created in the `Admin` role on the first run. The password is not stored in source control. Normal users can create accounts from the registration page; account passwords are hashed by ASP.NET Core Identity.

To use another database file, set `ConnectionStrings__FleetDatabase`, for example:

```powershell
$env:ConnectionStrings__FleetDatabase = "Data Source=C:\data\driveops.db"
```

Authenticated users can create, view, update, and delete vehicles, drivers, fuel records, and maintenance records. A vehicle with fuel or maintenance history cannot be deleted.
