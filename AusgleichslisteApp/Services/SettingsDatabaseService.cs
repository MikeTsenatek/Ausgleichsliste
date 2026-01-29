using AusgleichslisteApp.Data;
using AusgleichslisteApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AusgleichslisteApp.Services
{
    /// <summary>
    /// Interface für erweiterte Settings-Verwaltung mit Datenbank
    /// </summary>
    public interface ISettingsDatabaseService
    {
        /// <summary>
        /// Lädt ein Setting aus der Datenbank
        /// </summary>
        Task<string?> GetSettingAsync(string key, string category = "");
        
        /// <summary>
        /// Speichert ein Setting in der Datenbank
        /// </summary>
        Task SetSettingAsync(string key, string value, string category = "", string description = "");
        
        /// <summary>
        /// Lädt alle Settings einer Kategorie
        /// </summary>
        Task<Dictionary<string, string>> GetSettingsByCategoryAsync(string category);
        
        /// <summary>
        /// Löscht ein Setting aus der Datenbank
        /// </summary>
        Task DeleteSettingAsync(string key, string category = "");
        
        /// <summary>
        /// Lädt komplette ApplicationSettings aus der Datenbank (mit Fallback auf appsettings.json)
        /// </summary>
        Task<ApplicationSettings> GetApplicationSettingsAsync();
        
        /// <summary>
        /// Speichert BrandingSettings in der Datenbank
        /// </summary>
        Task SaveBrandingSettingsAsync(BrandingSettings brandingSettings);
        
        /// <summary>
        /// Lädt BrandingSettings aus der Datenbank (mit Fallback)
        /// </summary>
        Task<BrandingSettings> GetBrandingSettingsAsync();
    }
    
    /// <summary>
    /// Service für erweiterte Settings-Verwaltung mit Datenbankunterstützung
    /// </summary>
    public class SettingsDatabaseService : ISettingsDatabaseService
    {
        private readonly AusgleichslisteDbContext _context;
        private readonly IOptionsMonitor<ApplicationSettings> _optionsMonitor;
        private readonly ILogger<SettingsDatabaseService> _logger;
        
        public SettingsDatabaseService(
            AusgleichslisteDbContext context,
            IOptionsMonitor<ApplicationSettings> optionsMonitor,
            ILogger<SettingsDatabaseService> logger)
        {
            _context = context;
            _optionsMonitor = optionsMonitor;
            _logger = logger;
        }
        
        public async Task<string?> GetSettingAsync(string key, string category = "")
        {
            try
            {
                var setting = await _context.ApplicationSettings
                    .FirstOrDefaultAsync(s => s.Key == key && s.Category == category);
                
                return setting?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des Settings {Key}/{Category}", key, category);
                return null;
            }
        }
        
        public async Task SetSettingAsync(string key, string value, string category = "", string description = "")
        {
            try
            {
                var existingSetting = await _context.ApplicationSettings
                    .FirstOrDefaultAsync(s => s.Key == key && s.Category == category);
                
                if (existingSetting != null)
                {
                    existingSetting.Value = value;
                    existingSetting.UpdatedAt = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(description))
                        existingSetting.Description = description;
                }
                else
                {
                    var newSetting = new ApplicationSetting(key, value, category, description);
                    _context.ApplicationSettings.Add(newSetting);
                }
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Setting {Key}/{Category} erfolgreich gespeichert", key, category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern des Settings {Key}/{Category}", key, category);
                throw;
            }
        }
        
        public async Task<Dictionary<string, string>> GetSettingsByCategoryAsync(string category)
        {
            try
            {
                var settings = await _context.ApplicationSettings
                    .Where(s => s.Category == category)
                    .ToDictionaryAsync(s => s.Key, s => s.Value);
                
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Settings für Kategorie {Category}", category);
                return new Dictionary<string, string>();
            }
        }
        
        public async Task DeleteSettingAsync(string key, string category = "")
        {
            try
            {
                var setting = await _context.ApplicationSettings
                    .FirstOrDefaultAsync(s => s.Key == key && s.Category == category);
                
                if (setting != null)
                {
                    _context.ApplicationSettings.Remove(setting);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Setting {Key}/{Category} erfolgreich gelöscht", key, category);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen des Settings {Key}/{Category}", key, category);
                throw;
            }
        }
        
        public async Task<ApplicationSettings> GetApplicationSettingsAsync()
        {
            try
            {
                // Starte mit Default-Settings aus appsettings.json
                var settings = new ApplicationSettings
                {
                    Branding = new BrandingSettings
                    {
                        ApplicationName = _optionsMonitor.CurrentValue.Branding.ApplicationName,
                        OrganizationUrl = _optionsMonitor.CurrentValue.Branding.OrganizationUrl,
                        OrganizationName = _optionsMonitor.CurrentValue.Branding.OrganizationName,
                        PrimaryColor = _optionsMonitor.CurrentValue.Branding.PrimaryColor,
                        SecondaryColor = _optionsMonitor.CurrentValue.Branding.SecondaryColor,
                        ShowLogo = _optionsMonitor.CurrentValue.Branding.ShowLogo,
                        LogoMaxHeight = _optionsMonitor.CurrentValue.Branding.LogoMaxHeight
                    },
                    Currency = _optionsMonitor.CurrentValue.Currency,
                    DateCulture = _optionsMonitor.CurrentValue.DateCulture,
                    ItemsPerPage = _optionsMonitor.CurrentValue.ItemsPerPage,
                    ShowDebugInfo = _optionsMonitor.CurrentValue.ShowDebugInfo
                };
                
                // Überschreibe mit Datenbank-Werten falls vorhanden
                var brandingSettings = await GetSettingsByCategoryAsync("Branding");
                foreach (var kvp in brandingSettings)
                {
                    switch (kvp.Key.ToLower())
                    {
                        case "applicationname":
                            settings.Branding.ApplicationName = kvp.Value;
                            break;

                        case "organizationurl":
                            settings.Branding.OrganizationUrl = kvp.Value;
                            break;
                        case "organizationname":
                            settings.Branding.OrganizationName = kvp.Value;
                            break;
                        case "primarycolor":
                            settings.Branding.PrimaryColor = kvp.Value;
                            break;
                        case "secondarycolor":
                            settings.Branding.SecondaryColor = kvp.Value;
                            break;
                        case "showlogo":
                            if (bool.TryParse(kvp.Value, out var showLogo))
                                settings.Branding.ShowLogo = showLogo;
                            break;
                        case "logomaxheight":
                            if (int.TryParse(kvp.Value, out var maxHeight))
                                settings.Branding.LogoMaxHeight = maxHeight;
                            break;
                    }
                }
                
                // Überschreibe allgemeine Settings
                var generalSettings = await GetSettingsByCategoryAsync("General");
                foreach (var kvp in generalSettings)
                {
                    switch (kvp.Key.ToLower())
                    {
                        case "currency":
                            settings.Currency = kvp.Value;
                            break;
                        case "dateculture":
                            settings.DateCulture = kvp.Value;
                            break;
                        case "itemsperpage":
                            if (int.TryParse(kvp.Value, out var itemsPerPage))
                                settings.ItemsPerPage = itemsPerPage;
                            break;
                        case "showdebuginfo":
                            if (bool.TryParse(kvp.Value, out var showDebugInfo))
                                settings.ShowDebugInfo = showDebugInfo;
                            break;
                    }
                }
                
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der ApplicationSettings aus der Datenbank");
                return _optionsMonitor.CurrentValue; // Fallback auf appsettings.json
            }
        }
        
        public async Task SaveBrandingSettingsAsync(BrandingSettings brandingSettings)
        {
            try
            {
                await SetSettingAsync("ApplicationName", brandingSettings.ApplicationName, "Branding", "Name der Anwendung");
    
                await SetSettingAsync("OrganizationUrl", brandingSettings.OrganizationUrl ?? "", "Branding", "URL der Organisation");
                await SetSettingAsync("OrganizationName", brandingSettings.OrganizationName ?? "", "Branding", "Name der Organisation");
                await SetSettingAsync("PrimaryColor", brandingSettings.PrimaryColor, "Branding", "Primärfarbe");
                await SetSettingAsync("SecondaryColor", brandingSettings.SecondaryColor, "Branding", "Sekundärfarbe");
                await SetSettingAsync("ShowLogo", brandingSettings.ShowLogo.ToString(), "Branding", "Logo anzeigen");
                await SetSettingAsync("LogoMaxHeight", brandingSettings.LogoMaxHeight.ToString(), "Branding", "Maximale Logo-Höhe in Pixel");
                
                _logger.LogInformation("Branding-Settings erfolgreich in Datenbank gespeichert");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern der Branding-Settings");
                throw;
            }
        }
        
        public async Task<BrandingSettings> GetBrandingSettingsAsync()
        {
            try
            {
                var settings = await GetApplicationSettingsAsync();
                return settings.Branding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Branding-Settings");
                return _optionsMonitor.CurrentValue.Branding; // Fallback
            }
        }
    }
}