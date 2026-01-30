using AusgleichslisteApp.Data;
using AusgleichslisteApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AusgleichslisteApp.Services
{
    /// <summary>
    /// Entity Framework Core basierte Implementierung des IDataService
    /// </summary>
    public class EfDataService : IDataService
    {
        private readonly AusgleichslisteDbContext _context;
        private readonly ILogger<EfDataService> _logger;
        
        public EfDataService(AusgleichslisteDbContext context, ILogger<EfDataService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<List<User>> GetUsersAsync()
        {
            try
            {
                return await _context.Users
                    .OrderBy(u => u.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Benutzer");
                throw;
            }
        }
        
        public async Task<List<Booking>> GetBookingsAsync()
        {
            try
            {
                var bookings = await _context.Bookings
                    .Where(b => !b.IsDeleted) // Nur nicht-gelöschte Buchungen
                    .OrderByDescending(b => b.Date)
                    .ThenByDescending(b => b.CreatedAt)
                    .ToListAsync();
                
                // Lade Users separat für Navigation Properties
                var users = await _context.Users.ToListAsync();
                var userMap = users.ToDictionary(u => u.Id, u => u);
                
                // Setze Navigation Properties manuell
                foreach (var booking in bookings)
                {
                    booking.Payer = userMap.GetValueOrDefault(booking.PayerId);
                    booking.Beneficiary = userMap.GetValueOrDefault(booking.BeneficiaryId);
                }
                
                return bookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Buchungen");
                throw;
            }
        }
        
        public async Task<List<Booking>> GetAllBookingsIncludingDeletedAsync()
        {
            try
            {
                var bookings = await _context.Bookings
                    .OrderByDescending(b => b.Date)
                    .ThenByDescending(b => b.CreatedAt)
                    .ToListAsync();
                
                // Lade Users separat für Navigation Properties
                var users = await _context.Users.ToListAsync();
                var userMap = users.ToDictionary(u => u.Id, u => u);
                
                // Setze Navigation Properties manuell
                foreach (var booking in bookings)
                {
                    booking.Payer = userMap.GetValueOrDefault(booking.PayerId);
                    booking.Beneficiary = userMap.GetValueOrDefault(booking.BeneficiaryId);
                }
                
                return bookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden aller Buchungen");
                throw;
            }
        }
        
        public async Task SaveUsersAsync(List<User> users)
        {
            try
            {
                // Diese Methode wird für Kompatibilität beibehalten, aber EF Core
                // verwaltet Änderungen automatisch über Add/Update/Delete Methoden
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern der Benutzer");
                throw;
            }
        }
        
        public async Task SaveBookingsAsync(List<Booking> bookings)
        {
            try
            {
                // Diese Methode wird für Kompatibilität beibehalten, aber EF Core
                // verwaltet Änderungen automatisch über Add/Update/Delete Methoden
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern der Buchungen");
                throw;
            }
        }
        
        public async Task<User?> GetUserByIdAsync(string id)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des Benutzers mit ID: {UserId}", id);
                throw;
            }
        }
        
        public async Task AddUserAsync(User user)
        {
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Benutzer hinzugefügt: {UserName} (ID: {UserId})", user.Name, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Hinzufügen des Benutzers: {UserName}", user.Name);
                throw;
            }
        }
        
        public async Task UpdateUserAsync(User user)
        {
            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Benutzer aktualisiert: {UserName} (ID: {UserId})", user.Name, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Aktualisieren des Benutzers: {UserName}", user.Name);
                throw;
            }
        }
        
        public async Task DeleteUserAsync(string id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user != null)
                {
                    // Prüfe, ob Benutzer in Buchungen verwendet wird
                    var hasBookings = await _context.Bookings
                        .AnyAsync(b => b.PayerId == id || b.BeneficiaryId == id);
                    
                    if (hasBookings)
                    {
                        // Deaktiviere Benutzer statt zu löschen, wenn Buchungen existieren
                        user.IsActive = false;
                        _context.Users.Update(user);
                        _logger.LogInformation("Benutzer deaktiviert (hat Buchungen): {UserName} (ID: {UserId})", user.Name, id);
                    }
                    else
                    {
                        // Lösche Benutzer nur wenn keine Buchungen existieren
                        _context.Users.Remove(user);
                        _logger.LogInformation("Benutzer gelöscht: {UserName} (ID: {UserId})", user.Name, id);
                    }
                    
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen des Benutzers mit ID: {UserId}", id);
                throw;
            }
        }
        
        public async Task AddBookingAsync(Booking booking)
        {
            try
            {
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Buchung hinzugefügt: {Article} - {Amount:F2}€ (ID: {BookingId})", 
                    booking.Article, booking.Amount, booking.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Hinzufügen der Buchung: {Article}", booking.Article);
                throw;
            }
        }
        
        public async Task DeleteBookingAsync(Guid id)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(id);
                if (booking != null)
                {
                    // Soft Delete: Markiere als gelöscht statt physikalisch zu löschen
                    booking.IsDeleted = true;
                    booking.DeletedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Buchung als gelöscht markiert: {Article} - {Amount:F2}€ (ID: {BookingId})", 
                        booking.Article, booking.Amount, id);
                }
                else
                {
                    _logger.LogWarning("Buchung zum Löschen nicht gefunden: ID {BookingId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen der Buchung mit ID: {BookingId}", id);
                throw;
            }
        }

        public async Task<Logo?> GetLogoAsync()
        {
            try
            {
                return await _context.Logos.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting logo");
                throw;
            }
        }

        public async Task SaveLogoAsync(Logo logo)
        {
            try
            {
                // Remove existing logo first
                var existingLogo = await _context.Logos.FirstOrDefaultAsync();
                if (existingLogo != null)
                {
                    _context.Logos.Remove(existingLogo);
                }

                // Add new logo
                logo.Id = 1; // Always use ID 1 for the single logo
                _context.Logos.Add(logo);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Logo saved: {FileName} ({FileSize} bytes)", logo.FileName, logo.FileSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving logo");
                throw;
            }
        }

        public async Task DeleteLogoAsync()
        {
            try
            {
                var logo = await _context.Logos.FirstOrDefaultAsync();
                if (logo != null)
                {
                    _context.Logos.Remove(logo);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Logo deleted");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting logo");
                throw;
            }
        }
    }
}