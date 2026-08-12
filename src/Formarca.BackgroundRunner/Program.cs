using Formarca.Application;
using Formarca.BackgroundRunner.Jobs;
using Formarca.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog(configuration => configuration
        .WriteTo.Console()
        .Enrich.FromLogContext());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddHostedService<RefreshTokenCleanupJob>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "BackgroundRunner terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
