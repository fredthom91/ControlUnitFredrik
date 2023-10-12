using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AzureFunctionFredrik.Data;

public class SqlContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private readonly IConfiguration _configuration;

    public SqlContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public SqlContextFactory()
    {
    }

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(
            "Server = localhost; Database = iotDevice; Trusted_Connection = True; TrustServerCertificate = true");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}