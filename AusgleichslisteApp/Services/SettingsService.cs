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
        /// Speichert Einstellungen (jetzt in Datenbank)
        /// </summary>
        Task SaveSettingsAsync(ApplicationSettings settings);
        

        

        
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
        private readonly ISettingsDatabaseService _settingsDatabaseService;
        private ApplicationSettings? _cachedSettings;
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5);
        
        public SettingsService(
            IOptionsMonitor<ApplicationSettings> optionsMonitor, 
            IConfiguration configuration,
            ILogger<SettingsService> logger,
            ISettingsDatabaseService settingsDatabaseService)
        {
            _optionsMonitor = optionsMonitor;
            _configuration = configuration;
            _logger = logger;
            _settingsDatabaseService = settingsDatabaseService;
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
                
                // Wenn Cache abgelaufen ist, versuche async zu laden
                Task.Run(async () =>
                {
                    try
                    {
                        _cachedSettings = await _settingsDatabaseService.GetApplicationSettingsAsync();
                        _lastCacheUpdate = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Fehler beim Laden der Settings aus der Datenbank, verwende appsettings.json");
                    }
                });
                
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
                
                // Lade aus Datenbank
                _cachedSettings = await _settingsDatabaseService.GetApplicationSettingsAsync();
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
                _cachedSettings = await _settingsDatabaseService.GetApplicationSettingsAsync();
                _lastCacheUpdate = DateTime.UtcNow;
                _logger.LogInformation("Settings erfolgreich aus Datenbank neu geladen");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Neuladen der Settings");
            }
        }
        
        public async Task SaveSettingsAsync(ApplicationSettings settings)
        {
            try
            {
                await _settingsDatabaseService.SaveBrandingSettingsAsync(settings.Branding);
                
                // Speichere auch allgemeine Settings
                await _settingsDatabaseService.SetSettingAsync("Currency", settings.Currency, "General", "Währungssymbol");
                await _settingsDatabaseService.SetSettingAsync("DateCulture", settings.DateCulture, "General", "Datumskultur");
                await _settingsDatabaseService.SetSettingAsync("ItemsPerPage", settings.ItemsPerPage.ToString(), "General", "Einträge pro Seite");
                await _settingsDatabaseService.SetSettingAsync("ShowDebugInfo", settings.ShowDebugInfo.ToString(), "General", "Debug-Informationen anzeigen");
                
                // Cache leeren
                _cachedSettings = null;
                _lastCacheUpdate = DateTime.MinValue;
                
                _logger.LogInformation("Einstellungen erfolgreich in der Datenbank gespeichert");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern der Settings in der Datenbank");
                throw;
            }
        }
    }
}