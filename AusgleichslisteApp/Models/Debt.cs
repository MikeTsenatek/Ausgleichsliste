namespace AusgleichslisteApp.Models
{
    /// <summary>
    /// Repräsentiert eine Schuld zwischen zwei Benutzern (Kante im Schuldgraph)
    /// </summary>
    public class Debt
    {
        public string DebtorId { get; set; } = string.Empty;  // wer schuldet
        public string CreditorId { get; set; } = string.Empty; // wem geschuldet wird
        public decimal Amount { get; set; } // Betrag der Schuld
        
        public User? Debtor { get; set; }
        public User? Creditor { get; set; }
        
        public Debt() { }
        
        public Debt(string debtorId, string creditorId, decimal amount)
        {
            DebtorId = debtorId;
            CreditorId = creditorId;
            Amount = Math.Round(amount, 2);
        }
    }
}