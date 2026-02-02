using AusgleichslisteApp.Components;
using AusgleichslisteApp.Data;
using AusgleichslisteApp.Models;
using AusgleichslisteApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
        .Build())
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Konfiguriere Anwendungseinstellungen
builder.Services.Configure<ApplicationSettings>(
    builder.Configuration.GetSection("ApplicationSettings"));

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// Entity Framework Core Configuration
builder.Services.AddDbContextFactory<AusgleichslisteDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=ausgleichsliste.db";
    options.UseSqlite(connectionString);
    
    // Logging für Development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.LogTo(Console.WriteLine, LogLevel.Information);
    }
});

// Normaler DbContext für andere Services - aber erst bei Bedarf registriert
builder.Services.AddScoped<AusgleichslisteDbContext>(provider =>
{
    var factory = provider.GetRequiredService<IDbContextFactory<AusgleichslisteDbContext>>();
    return factory.CreateDbContext();
});

// Registriere unsere Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDataService, EfDataService>();
builder.Services.AddScoped<ISettlementService, SettlementService>();
builder.Services.AddScoped<ISettingsDatabaseService, SettingsDatabaseService>();
builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddScoped<ILogoService, LogoService>();
builder.Services.AddScoped<ISettingsCacheService, SettingsCacheService>();
builder.Services.AddScoped<IExpressionCalculatorService, ExpressionCalculatorService>();

var app = builder.Build();

// Automatische Datenbank-Migration und Initialisierung
using (var scope = app.Services.CreateScope())
{
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AusgleichslisteDbContext>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        using var context = contextFactory.CreateDbContext();
        
        // Führe alle ausstehenden Migrationen aus
        await context.Database.MigrateAsync();
        logger.LogInformation("Datenbank-Migrationen erfolgreich angewandt");
        
        logger.LogInformation("Anwendung bereit - Settings werden bei Bedarf geladen");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Fehler bei der Datenbank-Initialisierung");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

try
{
    Log.Information("Starting AusgleichslisteApp");
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
