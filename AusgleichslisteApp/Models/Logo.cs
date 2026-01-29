namespace AusgleichslisteApp.Models
{
    /// <summary>
    /// Model für das hochgeladene Logo
    /// </summary>
    public class Logo
    {
        public int Id { get; set; } = 1; // Immer nur ein Logo
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public DateTime UploadedAt { get; set; } = DateTime.Now;
        public long FileSize { get; set; }
        
        public Logo() { }
        
        public Logo(string fileName, string contentType, byte[] data)
        {
            FileName = fileName;
            ContentType = contentType;
            Data = data;
            FileSize = data.Length;
            UploadedAt = DateTime.Now;
        }
    }
}