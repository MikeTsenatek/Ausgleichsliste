using AusgleichslisteApp.Models;

namespace AusgleichslisteApp.Services
{
    /// <summary>
    /// Interface für Datenzugriff - unterstützt verschiedene Implementierungen (SQLite, PostgreSQL, etc.)
    /// </summary>
    public interface IDataService
    {
        Task<List<User>> GetUsersAsync();
        Task<List<Booking>> GetBookingsAsync();
        Task<List<Booking>> GetAllBookingsIncludingDeletedAsync(); // Für Admin-Zwecke
        Task<List<Booking>> GetFilteredBookingsAsync(string? searchText = null, string? payerId = null, string? beneficiaryId = null, DateTime? dateFrom = null, DateTime? dateTo = null, bool includeDeleted = false);
        Task SaveUsersAsync(List<User> users);
        Task SaveBookingsAsync(List<Booking> bookings);
        Task<User?> GetUserByIdAsync(string id);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(string id);
        Task AddBookingAsync(Booking booking);
        Task DeleteBookingAsync(Guid id);
        Task<Logo?> GetLogoAsync();
        Task SaveLogoAsync(Logo logo);
        Task DeleteLogoAsync();
    }
}