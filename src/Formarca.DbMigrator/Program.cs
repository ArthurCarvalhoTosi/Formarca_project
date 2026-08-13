using Formarca.Application;
using Formarca.Infrastructure;
using Formarca.Infrastructure.Persistence;
using Formarca.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

IHost? host = null;
try
{
    // Console hosts default ContentRoot to the current working directory.
    // `dotnet run --project ...` from the repo root would miss appsettings.json.
    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        Args = args,
        ContentRootPath = AppContext.BaseDirectory
    });

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddEnvironmentVariables();

    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
    {
        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not configured. " +
            "Copy src/Formarca.DbMigrator/appsettings.json.example to appsettings.json and set the SQL Server connection string.");
    }

    builder.Services.AddSerilog(configuration => configuration
        .WriteTo.Console()
        .Enrich.FromLogContext());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    host = builder.Build();

    Log.Information("Applying EF Core migrations...");
    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    Log.Information("Seeding database...");
    await DatabaseSeeder.SeedAsync(host.Services);

    Log.Information("DbMigrator completed successfully.");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "DbMigrator failed");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
    if (host is IAsyncDisposable asyncDisposable)
    {
        await asyncDisposable.DisposeAsync();
    }
    else
    {
        host?.Dispose();
    }
}
