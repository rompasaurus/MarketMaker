using Microsoft.EntityFrameworkCore;
using PolymarketTracker.Api.Data;
using PolymarketTracker.Api.Hubs;
using PolymarketTracker.Api.Configuration;
using PolymarketTracker.Api.Services;
using PolymarketTracker.Api.Services.Interfaces;
using PolymarketTracker.Api.Workers;
using PolymarketTracker.Api.Middleware;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter())
        .WriteTo.File(
            new Serilog.Formatting.Compact.CompactJsonFormatter(),
            "logs/log-.json",
            rollingInterval: RollingInterval.Day));

    // Configuration
    builder.Services.Configure<PolymarketOptions>(
        builder.Configuration.GetSection("Polymarket"));
    builder.Services.Configure<GdeltOptions>(
        builder.Configuration.GetSection("Gdelt"));

    // Database
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // HttpClients
    builder.Services.AddHttpClient<IPolymarketService, PolymarketApiService>(client =>
    {
        client.BaseAddress = new Uri("https://clob.polymarket.com");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    builder.Services.AddHttpClient<INewsService, GdeltNewsService>(client =>
    {
        client.BaseAddress = new Uri("https://api.gdeltproject.org");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    builder.Services.AddHttpClient("GammaApi", client =>
    {
        client.BaseAddress = new Uri("https://gamma-api.polymarket.com");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    // Services
    builder.Services.AddScoped<IIssueService, IssueService>();

    // Background workers
    builder.Services.AddHostedService<MarketSyncWorker>();
    builder.Services.AddHostedService<PriceSnapshotWorker>();
    builder.Services.AddHostedService<PolymarketWebSocketWorker>();
    builder.Services.AddHostedService<NewsSyncWorker>();

    // SignalR
    builder.Services.AddSignalR();

    // Controllers
    builder.Services.AddControllers();

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Health checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

    var app = builder.Build();

    // Apply migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    // Middleware
    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseCors();

    app.MapControllers();
    app.MapHub<MarketHub>("/hubs/market");
    app.MapHealthChecks("/api/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
