using Microsoft.EntityFrameworkCore;
using AusgleichslisteApp.Models;

namespace AusgleichslisteApp.Data
{
    /// <summary>
    /// Entity Framework DbContext für die Ausgleichsliste-Anwendung
    /// </summary>
    public class AusgleichslisteDbContext : DbContext
    {
        public AusgleichslisteDbContext(DbContextOptions<AusgleichslisteDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Booking> Bookings { get; set; } = default!;
        public DbSet<Logo> Logos { get; set; } = default!;
        public DbSet<ConfigurationEntry> ApplicationSettings { get; set; } = default!;
        public DbSet<Settlement> Settlements { get; set; } = default!;
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // User Konfiguration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.InitialName).HasMaxLength(200);
                entity.Property(e => e.PaymentMethod).HasMaxLength(500).IsRequired(false);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
                
                // Index für bessere Performance
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.IsActive);
            });
            
            // Booking Konfiguration
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.Article).IsRequired().HasMaxLength(500);
                entity.Property(e => e.PayerId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.BeneficiaryId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Amount).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsSettlement).IsRequired();
                
                // Fremdschlüssel-Beziehungen (ohne CASCADE DELETE für Flexibilität)
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.PayerId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.BeneficiaryId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Navigation Properties ignorieren (werden manuell gesetzt)
                entity.Ignore(e => e.Payer);
                entity.Ignore(e => e.Beneficiary);
                
                // Indizes für bessere Performance
                entity.HasIndex(e => e.Date);
                entity.HasIndex(e => e.PayerId);
                entity.HasIndex(e => e.BeneficiaryId);
                entity.HasIndex(e => e.IsSettlement);
                entity.HasIndex(e => e.CreatedAt);
            });
            
            // Logo Konfiguration
            modelBuilder.Entity<Logo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Data).IsRequired();
                entity.Property(e => e.UploadedAt).IsRequired();
                entity.Property(e => e.FileSize).IsRequired();
                
                // Index für bessere Performance
                entity.HasIndex(e => e.UploadedAt);
            });
            
            // ApplicationSettings Konfiguration
            modelBuilder.Entity<ConfigurationEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Value).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                
                // Index für bessere Performance und Eindeutigkeit
                entity.HasIndex(e => new { e.Key, e.Category }).IsUnique();
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.UpdatedAt);
            });

            // Settlement Konfiguration
            modelBuilder.Entity<Settlement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.PayerId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.RecipientId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Amount).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.SuggestedDate).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
                
                // Indizes für bessere Performance
                entity.HasIndex(e => e.PayerId);
                entity.HasIndex(e => e.RecipientId);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.SuggestedDate);
            });
        }
    }
}