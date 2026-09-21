namespace AusgleichslisteApp.Models
{
    /// <summary>
    /// Produkt, das über die Shopseite als Buchung gekauft werden kann.
    /// </summary>
    public class ShopProduct
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        public string ReceiverUserId { get; set; } = string.Empty;
        public byte[]? ImageData { get; set; }
        public string? ImageContentType { get; set; }
        public string? ImageFileName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public User? ReceiverUser { get; set; }

        public string? ImageDataUrl =>
            ImageData is { Length: > 0 } && !string.IsNullOrWhiteSpace(ImageContentType)
                ? $"data:{ImageContentType};base64,{Convert.ToBase64String(ImageData)}"
                : null;
    }
}
