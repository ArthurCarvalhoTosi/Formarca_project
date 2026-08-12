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

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddEnvironmentVariables();

    builder.Services.AddSerilog(configuration => configuration
        .WriteTo.Console()
        .Enrich.FromLogContext());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    using var host = builder.Build();

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
}
