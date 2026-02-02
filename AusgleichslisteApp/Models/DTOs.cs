namespace AusgleichslisteApp.Models
{
    /// <summary>
    /// DTO für das Hinzufügen neuer Buchungen
    /// </summary>
    public class AddBookingRequest
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public string Article { get; set; } = string.Empty;
        public string PayerId { get; set; } = string.Empty;
        public string BeneficiaryId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Article) &&
                   !string.IsNullOrEmpty(PayerId) &&
                   !string.IsNullOrEmpty(BeneficiaryId) &&
                   PayerId != BeneficiaryId &&
                   Amount > 0;
        }
    }
  
    /// <summary>
    /// DTO für detaillierte Sammel-Buchungen mit individuellen Beträgen
    /// </summary>
    public class DetailedMassBookingRequest
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public string Article { get; set; } = string.Empty;
        public string PayerId { get; set; } = string.Empty;
        public decimal ExpectedTotal { get; set; }
        public List<PersonAmount> PersonAmounts { get; set; } = new List<PersonAmount>();
        
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Article) &&
                   !string.IsNullOrEmpty(PayerId) &&
                   PersonAmounts.Any(p => p.IsSelected && p.Amount > 0);
        }
    }
    
    /// <summary>
    /// Repräsentiert den Betrag einer Person in einer Sammel-Buchung
    /// </summary>
    public class PersonAmount
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal Amount { get; set; }        public string Comment { get; set; } = string.Empty;        public bool IsSelected { get; set; }
    }
    
    /// <summary>
    /// DTO für das Hinzufügen neuer Benutzer
    /// </summary>
    public class AddUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name) && Name.Length >= 2;
        }
    }
}