using ClassLibraryFredrik.DataModels;
using Microsoft.EntityFrameworkCore;

namespace AzureFunctionFredrik.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<LampData> LampData { get; set; }
    public DbSet<DeviceRegistration> DeviceRegistrations { get; set; }
}