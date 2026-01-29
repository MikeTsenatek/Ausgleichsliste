namespace AusgleichslisteApp.Models
{
    /// <summary>
    /// Repräsentiert eine einzelne Buchung/Transaktion
    /// </summary>
    public class Booking
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Date { get; set; } = DateTime.Now;
        public string Article { get; set; } = string.Empty;
        public string PayerId { get; set; } = string.Empty; // wer hat bezahlt ("von")
        public string BeneficiaryId { get; set; } = string.Empty; // für wen ("für")
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsSettlement { get; set; } = false; // True für automatische Ausgleichsbuchungen
        
        /// <summary>
        /// Navigation properties (werden beim Laden gesetzt)
        /// </summary>
        public User? Payer { get; set; }
        public User? Beneficiary { get; set; }
        
        public Booking() { }
        
        public Booking(string payerId, string beneficiaryId, decimal amount, string article, DateTime? date = null)
        {
            Id = Guid.NewGuid();
            PayerId = payerId;
            BeneficiaryId = beneficiaryId;
            Amount = Math.Round(amount, 2);
            Article = article;
            Date = date ?? DateTime.Now;
            CreatedAt = DateTime.Now;
            IsSettlement = false;
        }
    }
}