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
        
        public async Task<List<Booking>> GetFilteredBookingsAsync(string? searchText = null, string? payerId = null, string? beneficiaryId = null, DateTime? dateFrom = null, DateTime? dateTo = null, bool includeDeleted = false)
        {
            try
            {
                var query = _context.Bookings.AsQueryable();
                
                // Gelöschte Buchungen einschließen oder ausschließen
                if (!includeDeleted)
                {
                    query = query.Where(b => !b.IsDeleted);
                }
                
                // Textsuche in Article
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var searchLower = searchText.ToLower();
                    query = query.Where(b => b.Article.ToLower().Contains(searchLower));
                }
                
                // Filter nach Zahler
                if (!string.IsNullOrWhiteSpace(payerId))
                {
                    query = query.Where(b => b.PayerId == payerId);
                }
                
                // Filter nach Empfänger
                if (!string.IsNullOrWhiteSpace(beneficiaryId))
                {
                    query = query.Where(b => b.BeneficiaryId == beneficiaryId);
                }
                
                // Filter nach Datum von
                if (dateFrom.HasValue)
                {
                    query = query.Where(b => b.Date.Date >= dateFrom.Value.Date);
                }
                
                // Filter nach Datum bis
                if (dateTo.HasValue)
                {
                    query = query.Where(b => b.Date.Date <= dateTo.Value.Date);
                }
                
                var bookings = await query
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
                _logger.LogError(ex, "Fehler beim Laden der gefilterten Buchungen");
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

        public async Task<List<Settlement>> GetActiveSettlementsAsync()
        {
            try
            {
                var settlements = await _context.Settlements
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SuggestedDate)
                    .ToListAsync();
                
                // Lade Users separat für Navigation Properties
                var users = await _context.Users.ToListAsync();
                var userMap = users.ToDictionary(u => u.Id, u => u);
                
                // Setze Navigation Properties manuell
                foreach (var settlement in settlements)
                {
                    if (userMap.TryGetValue(settlement.PayerId, out var payer))
                        settlement.Payer = payer;
                    if (userMap.TryGetValue(settlement.RecipientId, out var recipient))
                        settlement.Recipient = recipient;
                }
                
                return settlements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der aktiven Settlements");
                throw;
            }
        }

        public async Task SaveSettlementAsync(Settlement settlement)
        {
            try
            {
                var existingSettlement = await _context.Settlements
                    .FirstOrDefaultAsync(s => s.Id == settlement.Id);

                if (existingSettlement != null)
                {
                    existingSettlement.PayerId = settlement.PayerId;
                    existingSettlement.RecipientId = settlement.RecipientId;
                    existingSettlement.Amount = settlement.Amount;
                    existingSettlement.IsActive = settlement.IsActive;
                    _context.Settlements.Update(existingSettlement);
                }
                else
                {
                    _context.Settlements.Add(settlement);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Settlement saved: {PayerId} -> {RecipientId}: {Amount}€", 
                    settlement.PayerId, settlement.RecipientId, settlement.Amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern des Settlements");
                throw;
            }
        }

        public async Task SaveSettlementsAsync(List<Settlement> settlements)
        {
            try
            {
                // Füge alle neuen Settlements hinzu
                _context.Settlements.AddRange(settlements);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("{Count} Settlements gespeichert", settlements.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern der Settlements");
                throw;
            }
        }

        public async Task DeleteSettlementAsync(Guid id)
        {
            try
            {
                var settlement = await _context.Settlements.FindAsync(id);
                if (settlement != null)
                {
                    _context.Settlements.Remove(settlement);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Settlement gelöscht: {Id}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen des Settlements");
                throw;
            }
        }

        public async Task UpdateSettlementAmountAsync(Guid id, decimal newAmount)
        {
            try
            {
                var settlement = await _context.Settlements.FindAsync(id);
                if (settlement != null)
                {
                    settlement.Amount = newAmount;
                    if (newAmount <= 0)
                    {
                        settlement.IsActive = false; // Deaktiviere Settlement wenn Betrag 0 oder negativ
                    }
                    
                    _context.Settlements.Update(settlement);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Settlement Betrag aktualisiert: {Id} -> {Amount}€", id, newAmount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Aktualisieren des Settlement-Betrags");
                throw;
            }
        }

        public async Task ClearAllSettlementsAsync()
        {
            try
            {
                var settlements = await _context.Settlements.ToListAsync();
                _context.Settlements.RemoveRange(settlements);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Alle Settlements gelöscht");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen aller Settlements");
                throw;
            }
        }
    }
}