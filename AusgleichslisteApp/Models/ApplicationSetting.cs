namespace AusgleichslisteApp.Models
{
    /// <summary>
    /// Datenbank-Model für Anwendungseinstellungen
    /// </summary>
    public class ApplicationSetting
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public ApplicationSetting() { }
        
        public ApplicationSetting(string key, string value, string category = "", string description = "")
        {
            Key = key;
            Value = value;
            Category = category;
            Description = description;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}