using AusgleichslisteApp.Data;
using AusgleichslisteApp.Models;
using AusgleichslisteApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AusgleichslisteApp.Tests.Services;

public class EfDataServiceTests : IDisposable
{
    private readonly AusgleichslisteDbContext _context;
    private readonly Mock<ILogger<EfDataService>> _mockLogger;
    private readonly EfDataService _service;

    public EfDataServiceTests()
    {
        var options = new DbContextOptionsBuilder<AusgleichslisteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        _context = new AusgleichslisteDbContext(options);
        _mockLogger = new Mock<ILogger<EfDataService>>();
        _service = new EfDataService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnOrderedUsers()
    {
        // Arrange
        var users = new[]
        {
            new User { Name = "Alice", Id = "alice-add-booking", IsActive = true },
            new User { Name = "Bob", Id = "bob-add-booking", IsActive = true },
            new User { Name = "Charlie", Id = "charlie-add-booking", IsActive = true }
        };
        
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUsersAsync();

        // Result should be ordered by Name
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Alice");
        result[1].Name.Should().Be("Bob");
        result[2].Name.Should().Be("Charlie");
    }

    [Fact]
    public async Task GetUsersAsync_WhenException_ShouldLogErrorAndThrow()
    {
        // Arrange
        _context.Dispose(); // Force an exception

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _service.GetUsersAsync());
        
        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fehler beim Laden der Benutzer")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AddUserAsync_ShouldAddUserAndReturnIt()
    {
        // Arrange
        var user = new User { Name = "Test User", IsActive = true };

        // Act
        await _service.AddUserAsync(user);
        
        // Get the user back to verify it was added
        var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Name == "Test User");
        // Assert
        dbUser.Should().NotBeNull();
        dbUser!.Name.Should().Be("Test User");
        dbUser.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(2));
        dbUser!.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task GetBookingsAsync_ShouldReturnBookingsOrderedByDateDescending()
    {
        // Arrange
        var users = new[]
        {
            new User { Name = "Alice", Id = "alice-booking-test", IsActive = true },
            new User { Name = "Bob", Id = "bob-booking-test", IsActive = true }
        };
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        var user1Id = users[0].Id;
        var user2Id = users[1].Id;

        var bookings = new[]
        {
            new Booking(user1Id, user2Id, 10m, "Oldest") { Date = DateTime.Today.AddDays(-2), CreatedAt = DateTime.Now.AddDays(-2) },
            new Booking(user2Id, user1Id, 20m, "Newest") { Date = DateTime.Today, CreatedAt = DateTime.Now },
            new Booking(user1Id, user2Id, 15m, "Middle") { Date = DateTime.Today.AddDays(-1), CreatedAt = DateTime.Now.AddDays(-1) }
        };
        
        _context.Bookings.AddRange(bookings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetBookingsAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].Article.Should().Be("Newest");  // Most recent first
        result[1].Article.Should().Be("Middle");
        result[2].Article.Should().Be("Oldest");   // Oldest last
    }

    [Fact]
    public async Task AddBookingAsync_ShouldAddBookingAndReturnIt()
    {
        // Arrange
        var users = new[]
        {
            new User { Name = "User1", IsActive = true },
            new User { Name = "User2", IsActive = true }
        };
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        var user1Id = users[0].Id.ToString();
        var user2Id = users[1].Id.ToString();

        var booking = new Booking(user1Id, user2Id, 25.50m, "Test booking");

        // Act
        await _service.AddBookingAsync(booking);
        
        // Get the booking back to verify it was added
        var dbBooking = await _context.Bookings.FirstOrDefaultAsync(b => b.Article == "Test booking");

        

        dbBooking.Should().NotBeNull();
        dbBooking!.Article.Should().Be("Test booking");
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateExistingUser()
    {
        // Arrange
        var user = new User { Name = "Original Name", IsActive = true };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        var userId = user.Id;

        user.Name = "Updated Name";
        user.IsActive = false;

        // Act
        await _service.UpdateUserAsync(user);

        // Assert



        
        var dbUser = await _context.Users.FindAsync(userId);
        dbUser!.Name.Should().Be("Updated Name");
        dbUser.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldRemoveUser()
    {
        // Arrange
        var user = new User { Name = "To Delete", IsActive = true };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        var userId = user.Id;

        // Act
        await _service.DeleteUserAsync(user.Id);

        // Assert
        var dbUser = await _context.Users.FindAsync(userId);
        dbUser.Should().BeNull();
    }

    [Fact]
    public async Task DeleteBookingAsync_ShouldRemoveBooking()
    {
        // Arrange
        var users = new[]
        {
            new User { Name = "User1", IsActive = true },
            new User { Name = "User2", IsActive = true }
        };
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();
        
        var booking = new Booking(users[0].Id.ToString(), users[1].Id.ToString(), 10m, "To Delete");
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        var bookingId = booking.Id;

        // Act
        await _service.DeleteBookingAsync(booking.Id);

        // Assert - Booking sollte soft-deleted sein (IsDeleted = true)
        var dbBooking = await _context.Bookings.FindAsync(bookingId);
        dbBooking.Should().NotBeNull();
        dbBooking!.IsDeleted.Should().BeTrue();
        dbBooking.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AddShopProductAsync_ShouldAddProduct()
    {
        // Arrange
        var receiver = new User { Name = "Shop Owner", IsActive = true };
        _context.Users.Add(receiver);
        await _context.SaveChangesAsync();

        var product = new ShopProduct
        {
            Name = "Cola",
            Barcode = "123456789",
            Price = 1.5m,
            ReceiverUserId = receiver.Id
        };

        // Act
        await _service.AddShopProductAsync(product);

        // Assert
        var dbProduct = await _context.ShopProducts.FirstOrDefaultAsync(p => p.Barcode == "123456789");
        dbProduct.Should().NotBeNull();
        dbProduct!.Name.Should().Be("Cola");
        dbProduct.Price.Should().Be(1.5m);
        dbProduct.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetShopProductByBarcodeAsync_ShouldReturnActiveProductWithReceiver()
    {
        // Arrange
        var receiver = new User { Name = "Shop Owner", IsActive = true };
        _context.Users.Add(receiver);
        _context.ShopProducts.Add(new ShopProduct
        {
            Name = "Snack",
            Barcode = "987654321",
            Price = 2m,
            ReceiverUserId = receiver.Id
        });
        await _context.SaveChangesAsync();

        // Act
        var product = await _service.GetShopProductByBarcodeAsync("987654321");

        // Assert
        product.Should().NotBeNull();
        product!.Name.Should().Be("Snack");
        product.ReceiverUser.Should().NotBeNull();
        product.ReceiverUser!.Name.Should().Be("Shop Owner");
    }

    [Fact]
    public async Task DeleteShopProductAsync_ShouldDeactivateProduct()
    {
        // Arrange
        var receiver = new User { Name = "Shop Owner", IsActive = true };
        var product = new ShopProduct
        {
            Name = "Water",
            Barcode = "111",
            Price = 1m,
            ReceiverUserId = receiver.Id
        };

        _context.Users.Add(receiver);
        _context.ShopProducts.Add(product);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteShopProductAsync(product.Id);

        // Assert
        var dbProduct = await _context.ShopProducts.FindAsync(product.Id);
        dbProduct.Should().NotBeNull();
        dbProduct!.IsActive.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
