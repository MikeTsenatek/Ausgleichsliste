using AusgleichslisteApp.Models;
using Microsoft.Extensions.Options;

namespace AusgleichslisteApp.Services
{
    /// <summary>
    /// Interface für Settings-Verwaltung
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Aktuelle Anwendungseinstellungen (Datenbank + appsettings.json)
        /// </summary>
        Task<ApplicationSettings> GetSettingsAsync();

        /// <summary>
        /// Lädt Settings neu aus der Konfiguration
        /// </summary>
        Task ReloadSettingsAsync();

        /// <summary>
        /// Legacy-Property für Kompatibilität (lädt aus Datenbank)
        /// </summary>
        ApplicationSettings Settings { get; }
    }

    /// <summary>
    /// Service für Settings-Verwaltung mit Datenbank-Unterstützung
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly IOptionsMonitor<ApplicationSettings> _optionsMonitor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SettingsService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private ApplicationSettings? _cachedSettings;
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5);

        public SettingsService(
            IOptionsMonitor<ApplicationSettings> optionsMonitor,
            IConfiguration configuration,
            ILogger<SettingsService> logger,
            IServiceProvider serviceProvider)
        {
            _optionsMonitor = optionsMonitor;
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public ApplicationSettings Settings
        {
            get
            {
                // Für Kompatibilität: Versuche aus Cache zu laden, sonst Fallback auf appsettings.json
                if (_cachedSettings != null && DateTime.UtcNow - _lastCacheUpdate < _cacheTimeout)
                {
                    return _cachedSettings;
                }

                // Kein async Loading im Property - einfach appsettings.json zurückgeben
                // Der Cache wird bei der ersten async Verwendung geladen
                return _cachedSettings ?? _optionsMonitor.CurrentValue;
            }
        }

        public async Task<ApplicationSettings> GetSettingsAsync()
        {
            try
            {
                // Prüfe Cache
                if (_cachedSettings != null && DateTime.UtcNow - _lastCacheUpdate < _cacheTimeout)
                {
                    return _cachedSettings;
                }

                // Lade aus Datenbank - lazy Service-Auflösung
                using var scope = _serviceProvider.CreateScope();
                var settingsDatabaseService = scope.ServiceProvider.GetRequiredService<ISettingsDatabaseService>();
                _cachedSettings = await settingsDatabaseService.GetApplicationSettingsAsync();
                _lastCacheUpdate = DateTime.UtcNow;

                return _cachedSettings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Settings aus der Datenbank");
                return _optionsMonitor.CurrentValue; // Fallback
            }
        }

        public async Task ReloadSettingsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var settingsDatabaseService = scope.ServiceProvider.GetRequiredService<ISettingsDatabaseService>();
                _cachedSettings = await settingsDatabaseService.GetApplicationSettingsAsync();
                _lastCacheUpdate = DateTime.UtcNow;
                _logger.LogInformation("Settings erfolgreich aus Datenbank neu geladen");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Neuladen der Settings");
            }
        }
    }
}