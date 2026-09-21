using Formarca.Application.Common.Interfaces;
using Formarca.Domain.Common;
using Formarca.Infrastructure.Persistence;
using Formarca.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Formarca.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString(Directory.GetCurrentDirectory());

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null))
            .Options;

        ITenantContext tenantContext = new TenantContext();
        tenantContext.SetIgnoreTenantFilters(true);
        tenantContext.SetIgnoreSoftDeleteFilters(true);

        return new AppDbContext(options, tenantContext, new SystemClock());
    }

    private static string ResolveConnectionString(string startDirectory)
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var repoRoot = FindRepoRoot(startDirectory);
        return ReadConnectionString(Path.Combine(repoRoot, "src", "Formarca.DbMigrator"))
            ?? ReadConnectionString(Path.Combine(repoRoot, "src", "Formarca.Web"))
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. " +
                "Set ConnectionStrings__DefaultConnection, or add it to " +
                "src/Formarca.DbMigrator/appsettings.json or src/Formarca.Web/appsettings.json.");
    }

    private static string? ReadConnectionString(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return null;
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(directory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        return string.IsNullOrWhiteSpace(connectionString) ? null : connectionString;
    }

    private static string FindRepoRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Formarca.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not find Formarca.sln walking up from '{startDirectory}'.");
    }
}
