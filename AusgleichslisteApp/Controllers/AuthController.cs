using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AusgleichslisteApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("/api/login")]
        public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password, [FromForm] string? returnUrl = null)
        {
            try
            {
                var adminConfig = _configuration.GetSection("AdminUser");
                var configUsername = adminConfig["Username"];
                var configPassword = adminConfig["Password"];
                var displayName = adminConfig["DisplayName"] ?? "Administrator";

                if (string.IsNullOrEmpty(configUsername) || string.IsNullOrEmpty(configPassword))
                {
                    _logger.LogError("Admin user configuration is missing or incomplete");
                    TempData["ErrorMessage"] = "Systemkonfiguration fehlt. Bitte wenden Sie sich an den Administrator.";
                    return Redirect("/login");
                }

                if (username == configUsername && password == configPassword)
                {
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

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation("User {Username} logged in successfully with Admin role", username);
                    
                    var redirectUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
                    return Redirect(redirectUrl);
                }
                else
                {
                    _logger.LogWarning("Failed login attempt for user {Username}", username);
                    TempData["ErrorMessage"] = "Ungültige Anmeldedaten. Bitte überprüfen Sie Benutzername und Passwort.";
                    return Redirect("/login");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt for user {Username}", username);
                TempData["ErrorMessage"] = "Ein Fehler ist aufgetreten. Bitte versuchen Sie es erneut.";
                return Redirect("/login");
            }
        }

        [HttpPost("/api/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User logged out successfully");
            return Redirect("/login");
        }


    }
}