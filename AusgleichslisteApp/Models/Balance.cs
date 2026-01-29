namespace AusgleichslisteApp.Models
{
    /// <summary>
    /// Repräsentiert den Netto-Saldo eines Benutzers
    /// </summary>
    public class Balance
    {
        public string UserId { get; set; } = string.Empty;
        public decimal NetBalance { get; set; } // > 0: bekommt Geld, < 0: zahlt Geld
        public User? User { get; set; }
        
        public Balance() { }
        
        public Balance(string userId, decimal netBalance)
        {
            UserId = userId;
            NetBalance = Math.Round(netBalance, 2);
        }
        
        /// <summary>
        /// True wenn Person Geld bekommen soll
        /// </summary>
        public bool IsCreditor => NetBalance > 0;
        
        /// <summary>
        /// True wenn Person Geld zahlen muss
        /// </summary>
        public bool IsDebtor => NetBalance < 0;
        
        /// <summary>
        /// Absoluter Betrag ohne Vorzeichen
        /// </summary>
        public decimal AbsoluteAmount => Math.Abs(NetBalance);
    }
}