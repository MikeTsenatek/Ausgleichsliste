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

// Add MVC Controllers for Authentication
builder.Services.AddControllersWithViews(); // This includes TempData services

// Add distributed cache (required for sessions)
builder.Services.AddDistributedMemoryCache();

// Add session services for TempData
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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
builder.Services.AddDbContext<AusgleichslisteDbContext>(options =>
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

// Registriere unsere Services
builder.Services.AddScoped<IDataService, EfDataService>();
builder.Services.AddScoped<ISettlementService, SettlementService>();
builder.Services.AddScoped<ISettingsDatabaseService, SettingsDatabaseService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<ILogoService, LogoService>();
builder.Services.AddSingleton<ISettingsCacheService, SettingsCacheService>();

var app = builder.Build();

// Automatische Datenbank-Migration und Initialisierung
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AusgleichslisteDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var settingsCache = app.Services.GetRequiredService<ISettingsCacheService>();
    
    try
    {
        // Erstelle/Migriere Datenbank
        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("Datenbank erfolgreich initialisiert");
        
        // Lade Settings-Cache
        await settingsCache.RefreshSettingsAsync();
        logger.LogInformation("Settings-Cache initialisiert");
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

// Add session middleware for TempData support
app.UseSession();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add Controller routes
app.MapControllers();

// Debug endpoint to test authentication
app.MapGet("/auth/test", (HttpContext context) => 
{
    var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
    var userName = context.User?.Identity?.Name ?? "Anonymous";
    var roles = context.User?.Claims?.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();
    
    var claims = context.User?.Claims?.Select(c => new { c.Type, c.Value }).ToArray();
    var allClaims = claims != null ? claims.Cast<object>().ToList() : new List<object>();
    
    return Results.Ok(new 
    {
        IsAuthenticated = isAuthenticated,
        UserName = userName,
        Roles = roles,
        AllClaims = allClaims
    });
});

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
