using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Cinema.Api.Data;

public sealed class CinemaDbContextFactory : IDesignTimeDbContextFactory<CinemaDbContext>
{
    public CinemaDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<CinemaDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=cinema_db;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<CinemaDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new CinemaDbContext(options);
    }
}
