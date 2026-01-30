using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace AusgleichslisteApp.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password);
    Task LogoutAsync();
}

public record AuthResult(bool Success, string? ErrorMessage = null);

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IConfiguration configuration, 
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            if (httpContext == null)
            {
                _logger.LogError("HttpContext is null - cannot perform authentication");
                return new AuthResult(false, "Authentifizierungskontext nicht verfügbar. Die Seite muss neu geladen werden.");
            }

            var adminConfig = _configuration.GetSection("AdminUser");
            var configUsername = adminConfig["Username"];
            var configPassword = adminConfig["Password"];
            var displayName = adminConfig["DisplayName"] ?? "Administrator";

            if (string.IsNullOrEmpty(configUsername) || string.IsNullOrEmpty(configPassword))
            {
                _logger.LogError("Admin user configuration is missing or incomplete");
                return new AuthResult(false, "Systemkonfiguration fehlt. Bitte wenden Sie sich an den Administrator.");
            }

            if (username != configUsername || password != configPassword)
            {
                _logger.LogWarning("Failed login attempt for user {Username}", username);
                return new AuthResult(false, "Ungültige Anmeldedaten. Bitte überprüfen Sie Benutzername und Passwort.");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.GivenName, displayName),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("DisplayName", displayName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("User {Username} logged in successfully with Admin role", username);
            return new AuthResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login attempt for user {Username}", username);
            return new AuthResult(false, "Ein Fehler ist aufgetreten. Bitte versuchen Sie es erneut.");
        }
    }

    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext 
            ?? throw new InvalidOperationException("HttpContext ist nicht verfügbar");

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User logged out successfully");
    }
}
