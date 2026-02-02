using AusgleichslisteApp.Models;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace AusgleichslisteApp.Services
{
    /// <summary>
    /// Thread-safe Settings-Cache Service
    /// </summary>
    public interface ISettingsCacheService
    {
        ApplicationSettings GetSettings();
        Task RefreshSettingsAsync();
        bool IsLoaded { get; }
    }
    
    public class SettingsCacheService : ISettingsCacheService
    {
        private readonly IOptionsMonitor<ApplicationSettings> _optionsMonitor;
        private readonly ISettingsDatabaseService _settingsDatabaseService;
        private readonly ILogger<SettingsCacheService> _logger;
        
        private ApplicationSettings? _cachedSettings;
        private readonly object _cacheLock = new object();
        private bool _isLoading = false;
        private DateTime _lastUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(10);
        
        public bool IsLoaded => _cachedSettings != null;
        
        public SettingsCacheService(
            IOptionsMonitor<ApplicationSettings> optionsMonitor,
            ISettingsDatabaseService settingsDatabaseService,
            ILogger<SettingsCacheService> logger)
        {
            _optionsMonitor = optionsMonitor;
            _settingsDatabaseService = settingsDatabaseService;
            _logger = logger;
            
            // Initialer Cache-Load aus appsettings.json
            _cachedSettings = _optionsMonitor.CurrentValue;
        }
        
        public ApplicationSettings GetSettings()
        {
            // Wenn Cache aktuell ist, verwende ihn
            if (_cachedSettings != null && DateTime.UtcNow - _lastUpdate < _cacheLifetime)
            {
                return _cachedSettings;
            }
            
            // Wenn noch nie geladen oder zu alt, versuche sync zu laden
            if (_cachedSettings == null || DateTime.UtcNow - _lastUpdate > _cacheLifetime)
            {
                if (!_isLoading)
                {
                    // Versuche synchronen Load für bessere UX
                    try
                    {
                        RefreshSettingsAsync().Wait(TimeSpan.FromSeconds(2));
                    }
                    catch
                    {
                        // Falls sync load fehlschlägt, starte async
                        _ = Task.Run(RefreshSettingsAsync);
                    }
                }
            }
            
            // Fallback auf aktuellen Cache oder appsettings.json
            return _cachedSettings ?? _optionsMonitor.CurrentValue;
        }
        
        public async Task RefreshSettingsAsync()
        {
            if (_isLoading) return;
            
            lock (_cacheLock)
            {
                if (_isLoading) return;
                _isLoading = true;
            }
            
            try
            {
                // Lade Base-Settings aus appsettings.json
                var baseSettings = _optionsMonitor.CurrentValue;
                var newSettings = new ApplicationSettings
                {
                    Branding = new BrandingSettings
                    {
                        ApplicationName = baseSettings.Branding.ApplicationName,
                        OrganizationName = baseSettings.Branding.OrganizationName,
                        OrganizationUrl = baseSettings.Branding.OrganizationUrl,
                        LogoMaxHeight = baseSettings.Branding.LogoMaxHeight,
                        ShowLogo = baseSettings.Branding.ShowLogo,
                        PrimaryColor = baseSettings.Branding.PrimaryColor,
                        SecondaryColor = baseSettings.Branding.SecondaryColor
                    },
                    Currency = baseSettings.Currency,
                    DateCulture = baseSettings.DateCulture,
                    ItemsPerPage = baseSettings.ItemsPerPage,
                    ShowDebugInfo = baseSettings.ShowDebugInfo
                };
                
                // Überschreibe mit Datenbank-Werten
                try
                {
                    var dbBrandingSettings = await _settingsDatabaseService.GetSettingsByCategoryAsync("Branding");
                    foreach (var kvp in dbBrandingSettings)
                    {
                        switch (kvp.Key)
                        {
                            case "ApplicationName":
                                newSettings.Branding.ApplicationName = kvp.Value;
                                break;
                            case "OrganizationName":
                                newSettings.Branding.OrganizationName = kvp.Value;
                                break;
                            case "OrganizationUrl":
                                newSettings.Branding.OrganizationUrl = kvp.Value;
                                break;
                            case "LogoMaxHeight":
                                if (int.TryParse(kvp.Value, out var height))
                                    newSettings.Branding.LogoMaxHeight = height;
                                break;
                            case "ShowLogo":
                                if (bool.TryParse(kvp.Value, out var show))
                                    newSettings.Branding.ShowLogo = show;
                                break;
                            case "PrimaryColor":
                                newSettings.Branding.PrimaryColor = kvp.Value;
                                break;
                            case "SecondaryColor":
                                newSettings.Branding.SecondaryColor = kvp.Value;
                                break;
                        }
                    }
                    
                    // Handle other ApplicationSettings if needed
                    var dbGeneralSettings = await _settingsDatabaseService.GetSettingsByCategoryAsync("General");
                    foreach (var kvp in dbGeneralSettings)
                    {
                        switch (kvp.Key)
                        {
                            case "Currency":
                                newSettings.Currency = kvp.Value;
                                break;
                            case "DateCulture":
                                newSettings.DateCulture = kvp.Value;
                                break;
                            case "ItemsPerPage":
                                if (int.TryParse(kvp.Value, out var items))
                                    newSettings.ItemsPerPage = items;
                                break;
                            case "ShowDebugInfo":
                                if (bool.TryParse(kvp.Value, out var debug))
                                    newSettings.ShowDebugInfo = debug;
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Konnte Datenbank-Settings nicht laden, verwende appsettings.json");
                }
                
                // Aktualisiere Cache
                lock (_cacheLock)
                {
                    _cachedSettings = newSettings;
                    _lastUpdate = DateTime.UtcNow;
                }
                
                _logger.LogDebug("Settings-Cache erfolgreich aktualisiert");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Aktualisieren der Settings");
            }
            finally
            {
                lock (_cacheLock)
                {
                    _isLoading = false;
                }
            }
        }
    }
}