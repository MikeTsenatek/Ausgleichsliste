namespace AusgleichslisteApp.Models
{
    /// <summary>
    /// Repräsentiert eine vorgeschlagene Ausgleichszahlung
    /// </summary>
    public class Settlement
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Eindeutige ID für Datenbank
        public string PayerId { get; set; } = string.Empty; // wer zahlt
        public string RecipientId { get; set; } = string.Empty; // wer bekommt
        public decimal Amount { get; set; } // zu zahlender Betrag
        public DateTime SuggestedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true; // Ob der Settlement noch aktiv ist
        
        public User? Payer { get; set; }
        public User? Recipient { get; set; }
        
        public Settlement() { }
        
        public Settlement(string payerId, string recipientId, decimal amount)
        {
            PayerId = payerId;
            RecipientId = recipientId;
            Amount = Math.Round(amount, 2);
            SuggestedDate = DateTime.Now;
        }
        
        /// <summary>
        /// Konvertiert Settlement zu einer Buchung
        /// </summary>
        public Booking ToBooking(string articlePrefix = "Ausgleich")
        {
            return new Booking(
                payerId: PayerId,
                beneficiaryId: RecipientId, 
                amount: Amount,
                article: $"{articlePrefix} {Payer?.Name ?? PayerId}->{Recipient?.Name ?? RecipientId}",
                date: SuggestedDate
            )
            {
                IsSettlement = true
            };
        }
    }
}