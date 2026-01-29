namespace AusgleichslisteApp.Models
{
    /// <summary>
    /// Konfiguration für Branding-Einstellungen
    /// </summary>
    public class BrandingSettings
    {
        /// <summary>
        /// Name der Anwendung/Organisation
        /// </summary>
        public string ApplicationName { get; set; } = "Ausgleichsliste";
        
        /// <summary>
        /// Logo als Base64-String (data:image/png;base64,...)
        /// </summary>
    
        
        /// <summary>
        /// URL zur Organisation (optional)
        /// </summary>
        public string OrganizationUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Name der Organisation (optional)
        /// </summary>
        public string OrganizationName { get; set; } = string.Empty;
        
        /// <summary>
        /// Primärfarbe für das Theme (CSS Color)
        /// </summary>
        public string PrimaryColor { get; set; } = "#6c757d";
        
        /// <summary>
        /// Sekundärfarbe für das Theme (CSS Color)
        /// </summary>
        public string SecondaryColor { get; set; } = "#495057";
        
        /// <summary>
        /// Ob das Logo angezeigt werden soll
        /// </summary>
        public bool ShowLogo { get; set; } = true;
        
        /// <summary>
        /// Maximale Höhe des Logos in Pixeln
        /// </summary>
        public int LogoMaxHeight { get; set; } = 50;
    }
    
    /// <summary>
    /// Allgemeine Anwendungseinstellungen
    /// </summary>
    public class ApplicationSettings
    {
        /// <summary>
        /// Branding-Einstellungen
        /// </summary>
        public BrandingSettings Branding { get; set; } = new();
        
        /// <summary>
        /// Währung, die in der App verwendet wird
        /// </summary>
        public string Currency { get; set; } = "€";
        
        /// <summary>
        /// Kultur für Datumsformatierung
        /// </summary>
        public string DateCulture { get; set; } = "de-DE";
        
        /// <summary>
        /// Anzahl Elemente pro Seite in Tabellen
        /// </summary>
        public int ItemsPerPage { get; set; } = 100;
        
        /// <summary>
        /// Ob Debug-Informationen angezeigt werden sollen
        /// </summary>
        public bool ShowDebugInfo { get; set; } = false;
    }
}