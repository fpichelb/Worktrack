using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MySqlConnector;

namespace Worktrack.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "server=localhost;database=worktrack;user=worktrackuser;password=Passwort123;";
        connectionString = NormalizeConnectionString(connectionString, environment);

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 43)));

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string NormalizeConnectionString(string connectionString, string environment)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);

        var isLocalHost =
            string.Equals(builder.Server, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(builder.Server, "127.0.0.1", StringComparison.OrdinalIgnoreCase);

        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase) && isLocalHost)
        {
            builder.SslMode = MySqlSslMode.None;
        }

        return builder.ConnectionString;
    }
}
