namespace AusgleichslisteApp.Models
{
    /// <summary>
    /// Repräsentiert einen Benutzer im Ausgleichssystem
    /// </summary>
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string InitialName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        
        public User() { }
        
        public User(string id, string name)
        {
            Id = id;
            Name = name;
            InitialName = name;
            CreatedAt = DateTime.Now;
            IsActive = true;
        }
    }
}