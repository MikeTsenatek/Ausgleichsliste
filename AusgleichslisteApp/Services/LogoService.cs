using AusgleichslisteApp.Data;
using AusgleichslisteApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AusgleichslisteApp.Services
{
    /// <summary>
    /// Interface für Logo-Verwaltung
    /// </summary>
    public interface ILogoService
    {
        /// <summary>
        /// Speichert ein Logo in der Datenbank
        /// </summary>
        Task<Logo> SaveLogoAsync(string fileName, string contentType, byte[] data);
        
        /// <summary>
        /// Lädt das aktuelle Logo
        /// </summary>
        Task<Logo?> GetCurrentLogoAsync();
        
        /// <summary>
        /// Löscht das aktuelle Logo
        /// </summary>
        Task DeleteCurrentLogoAsync();
        
        /// <summary>
        /// Validiert ein Logo
        /// </summary>
        bool ValidateLogo(byte[] data, string contentType);
        
        /// <summary>
        /// Konvertiert Logo zu Base64 Data URL
        /// </summary>
        string ConvertToBase64DataUrl(Logo logo);
    }
    
    /// <summary>
    /// Service für Logo-Verwaltung
    /// </summary>
    public class LogoService : ILogoService
    {
        private readonly AusgleichslisteDbContext _context;
        private readonly ILogger<LogoService> _logger;
        
        // Erlaubte MIME-Types für Logos
        private static readonly string[] AllowedMimeTypes = 
        {
            "image/jpeg",
            "image/jpg",
            "image/png", 
            "image/gif",
            "image/webp",
            "image/svg+xml"
        };
        
        // Maximale Dateigröße (2MB)
        private const long MaxFileSize = 2 * 1024 * 1024;
        
        public LogoService(AusgleichslisteDbContext context, ILogger<LogoService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<Logo> SaveLogoAsync(string fileName, string contentType, byte[] data)
        {
            try
            {
                // Validierung
                if (!ValidateLogo(data, contentType))
                {
                    throw new ArgumentException("Invalid logo data or content type");
                }
                
                // Lösche eventuell vorhandenes Logo (wir haben immer nur eins mit ID = 1)
                await DeleteCurrentLogoAsync();
                
                // Erstelle neues Logo
                var logo = new Logo(fileName, contentType, data);
                
                _context.Logos.Add(logo);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Logo erfolgreich gespeichert: {FileName} ({FileSize} bytes)", 
                    fileName, data.Length);
                
                return logo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern des Logos: {FileName}", fileName);
                throw;
            }
        }
        
        public async Task<Logo?> GetCurrentLogoAsync()
        {
            try
            {
                // Da wir normalerweise nur ein Logo haben, nehmen wir das neueste
                var logo = await _context.Logos
                    .OrderByDescending(l => l.UploadedAt)
                    .FirstOrDefaultAsync();
                    
                return logo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des aktuellen Logos");
                return null;
            }
        }
        
        public async Task DeleteCurrentLogoAsync()
        {
            try
            {
                var existingLogos = await _context.Logos.ToListAsync();
                
                if (existingLogos.Any())
                {
                    _context.Logos.RemoveRange(existingLogos);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Alle vorhandenen Logos wurden gelöscht ({Count} Logos)", 
                        existingLogos.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen des aktuellen Logos");
                throw;
            }
        }
        
        public bool ValidateLogo(byte[] data, string contentType)
        {
            // Prüfe Dateigröße
            if (data.Length == 0 || data.Length > MaxFileSize)
            {
                _logger.LogWarning("Logo hat ungültige Dateigröße: {Size} bytes (max: {MaxSize})", 
                    data.Length, MaxFileSize);
                return false;
            }
            
            // Prüfe Content-Type
            if (string.IsNullOrWhiteSpace(contentType) || !AllowedMimeTypes.Contains(contentType.ToLower()))
            {
                _logger.LogWarning("Logo hat ungültigen Content-Type: {ContentType}", contentType);
                return false;
            }
            
            // Einfache Validierung der Dateimagic-Bytes
            if (!ValidateFileSignature(data, contentType))
            {
                _logger.LogWarning("Logo-Datei entspricht nicht dem angegebenen Content-Type: {ContentType}", contentType);
                return false;
            }
            
            return true;
        }
        
        public string ConvertToBase64DataUrl(Logo logo)
        {
            if (logo?.Data == null || logo.Data.Length == 0)
            {
                throw new ArgumentException("Logo hat keine gültigen Daten");
            }
            
            var base64 = Convert.ToBase64String(logo.Data);
            return $"data:{logo.ContentType};base64,{base64}";
        }
        
        /// <summary>
        /// Validiert die File-Signature (Magic Bytes) gegen den Content-Type
        /// </summary>
        private bool ValidateFileSignature(byte[] data, string contentType)
        {
            if (data.Length < 4)
                return false;
                
            return contentType.ToLower() switch
            {
                "image/jpeg" or "image/jpg" => data[0] == 0xFF && data[1] == 0xD8,
                "image/png" => data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47,
                "image/gif" => (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46),
                "image/webp" => data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
                               data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50,
                "image/svg+xml" => System.Text.Encoding.UTF8.GetString(data[..Math.Min(100, data.Length)]).Contains("<svg"),
                _ => true // Für unbekannte Typen keine strikte Validierung
            };
        }
    }
}