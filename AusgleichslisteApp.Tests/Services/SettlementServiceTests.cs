using AusgleichslisteApp.Models;
using AusgleichslisteApp.Services;
using Microsoft.Extensions.Logging;

namespace AusgleichslisteApp.Tests.Services;

public class SettlementServiceTests
{
    private readonly Mock<IDataService> _mockDataService;
    private readonly Mock<ILogger<SettlementService>> _mockLogger;
    private readonly SettlementService _service;

    public SettlementServiceTests()
    {
        _mockDataService = new Mock<IDataService>();
        _mockLogger = new Mock<ILogger<SettlementService>>();
        _service = new SettlementService(_mockDataService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CalculateBalancesAsync_WithSimpleBookings_ShouldCalculateCorrectBalances()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = "alice", Name = "Alice", IsActive = true },
            new User { Id = "bob", Name = "Bob", IsActive = true },
            new User { Id = "charlie", Name = "Charlie", IsActive = true }
        };

        var bookings = new List<Booking>
        {
            // Alice paid 30 for Bob -> Alice +30, Bob -30
            new Booking("alice", "bob", 30m, "Lunch"),
            // Bob paid 20 for Charlie -> Bob +20, Charlie -20
            new Booking("bob", "charlie", 20m, "Coffee"),
            // Charlie paid 10 for Alice -> Charlie +10, Alice -10
            new Booking("charlie", "alice", 10m, "Taxi")
        };

        _mockDataService.Setup(x => x.GetUsersAsync()).ReturnsAsync(users);
        _mockDataService.Setup(x => x.GetBookingsAsync()).ReturnsAsync(bookings);

        // Act
        var result = await _service.CalculateBalancesAsync();

        // Assert
        result.Should().HaveCount(3);

        var aliceBalance = result.First(b => b.UserId == "alice");
        aliceBalance.NetBalance.Should().Be(20m); // +30 -10 = 20

        var bobBalance = result.First(b => b.UserId == "bob");
        bobBalance.NetBalance.Should().Be(-10m); // -30 +20 = -10

        var charlieBalance = result.First(b => b.UserId == "charlie");
        charlieBalance.NetBalance.Should().Be(-10m); // -20 +10 = -10

        // Should be ordered by balance descending
        result[0].NetBalance.Should().Be(20m);  // Alice first (highest)
        result[1].NetBalance.Should().Be(-10m); // Bob or Charlie
        result[2].NetBalance.Should().Be(-10m); // Charlie or Bob
    }

    [Fact]
    public async Task CalculateBalancesAsync_WithInactiveUsers_ShouldIgnoreInactiveUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = "alice", Name = "Alice", IsActive = true },
            new User { Id = "bob", Name = "Bob", IsActive = false }, // Inactive
            new User { Id = "charlie", Name = "Charlie", IsActive = true }
        };

        var bookings = new List<Booking>
        {
            new Booking("alice", "bob", 30m, "Test"),
            new Booking("bob", "charlie", 20m, "Test")
        };

        _mockDataService.Setup(x => x.GetUsersAsync()).ReturnsAsync(users);
        _mockDataService.Setup(x => x.GetBookingsAsync()).ReturnsAsync(bookings);

        // Act
        var result = await _service.CalculateBalancesAsync();

        // Assert
        result.Should().HaveCount(2); // Only active users
        result.Should().NotContain(b => b.UserId == "bob");
        result.Should().Contain(b => b.UserId == "alice");
        result.Should().Contain(b => b.UserId == "charlie");
    }

    [Fact]
    public async Task CalculateMinimalTransfersAsync_WithSimpleScenario_ShouldReturnOptimalSettlements()
    {
        // Arrange - Alice owes 20, Bob is owed 20
        var users = new List<User>
        {
            new User { Id = "alice", Name = "Alice", IsActive = true },
            new User { Id = "bob", Name = "Bob", IsActive = true }
        };

        var bookings = new List<Booking>
        {
            // Bob paid 20 for Alice -> Alice owes Bob 20
            new Booking("bob", "alice", 20m, "Test")
        };

        _mockDataService.Setup(x => x.GetUsersAsync()).ReturnsAsync(users);
        _mockDataService.Setup(x => x.GetBookingsAsync()).ReturnsAsync(bookings);

        // Act
        var result = await _service.CalculateMinimalTransfersAsync();

        // Assert
        result.Should().HaveCount(1);
        
        var settlement = result.First();
        settlement.PayerId.Should().Be("alice");
        settlement.RecipientId.Should().Be("bob");
        settlement.Amount.Should().Be(20m);
    }

    [Fact]
    public async Task CalculateMinimalTransfersAsync_WithComplexScenario_ShouldMinimizeTransfers()
    {
        // Arrange - 3-way scenario where direct optimization is needed
        var users = new List<User>
        {
            new User { Id = "alice", Name = "Alice", IsActive = true },
            new User { Id = "bob", Name = "Bob", IsActive = true },
            new User { Id = "charlie", Name = "Charlie", IsActive = true }
        };

        var bookings = new List<Booking>
        {
            // Alice paid 30 for Bob
            new Booking("alice", "bob", 30m, "Test"),
            // Bob paid 20 for Charlie  
            new Booking("bob", "charlie", 20m, "Test")
            // Result: Alice +30, Bob -10, Charlie -20
        };

        _mockDataService.Setup(x => x.GetUsersAsync()).ReturnsAsync(users);
        _mockDataService.Setup(x => x.GetBookingsAsync()).ReturnsAsync(bookings);

        // Act
        var result = await _service.CalculateMinimalTransfersAsync();

        // Assert
        result.Should().HaveCount(2); // Should need 2 transfers to settle

        var totalSettlementAmount = result.Sum(s => s.Amount);
        totalSettlementAmount.Should().Be(30m); // Total amount should equal positive balances

        // Alice should receive money (she has positive balance)
        result.Should().NotContain(s => s.PayerId == "alice");
        result.Should().Contain(s => s.RecipientId == "alice");
    }

    [Fact]
    public async Task CalculateCurrentDebtsAsync_ShouldReturnDirectDebtsBetweenUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = "alice", Name = "Alice", IsActive = true },
            new User { Id = "bob", Name = "Bob", IsActive = true },
            new User { Id = "charlie", Name = "Charlie", IsActive = true }
        };

        var bookings = new List<Booking>
        {
            // Alice paid 15 for Bob
            new Booking("alice", "bob", 15m, "Test"),
            // Alice paid 10 for Bob (same pair - should aggregate)
            new Booking("alice", "bob", 10m, "Test"),
            // Bob paid 5 for Charlie
            new Booking("bob", "charlie", 5m, "Test")
        };

        _mockDataService.Setup(x => x.GetUsersAsync()).ReturnsAsync(users);
        _mockDataService.Setup(x => x.GetBookingsAsync()).ReturnsAsync(bookings);

        // Act
        var result = await _service.CalculateCurrentDebtsAsync();

        // Assert
        result.Should().HaveCount(2);

        // Bob owes Alice 25 (15 + 10)
        var bobOwesAlice = result.First(d => d.DebtorId == "bob" && d.CreditorId == "alice");
        bobOwesAlice.Amount.Should().Be(25m);

        // Charlie owes Bob 5
        var charlieOwesBob = result.First(d => d.DebtorId == "charlie" && d.CreditorId == "bob");
        charlieOwesBob.Amount.Should().Be(5m);

        // Should be ordered by amount descending
        result[0].Amount.Should().BeGreaterThanOrEqualTo(result[1].Amount);
    }

    [Fact]
    public async Task GetUserBalanceAsync_ShouldReturnCorrectBalance()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = "alice", Name = "Alice", IsActive = true },
            new User { Id = "bob", Name = "Bob", IsActive = true }
        };

        var bookings = new List<Booking>
        {
            new Booking("alice", "bob", 20m, "Test")
        };

        _mockDataService.Setup(x => x.GetUsersAsync()).ReturnsAsync(users);
        _mockDataService.Setup(x => x.GetBookingsAsync()).ReturnsAsync(bookings);

        // Act
        var aliceBalance = await _service.GetUserBalanceAsync("alice");
        var bobBalance = await _service.GetUserBalanceAsync("bob");
        var nonExistentBalance = await _service.GetUserBalanceAsync("unknown");

        // Assert
        aliceBalance.Should().Be(20m);
        bobBalance.Should().Be(-20m);
        nonExistentBalance.Should().Be(0m);
    }

    [Fact]
    public async Task CanDeleteUserAsync_WithZeroBalance_ShouldReturnTrue()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = "alice", Name = "Alice", IsActive = true }
        };

        _mockDataService.Setup(x => x.GetUsersAsync()).ReturnsAsync(users);
        _mockDataService.Setup(x => x.GetBookingsAsync()).ReturnsAsync(new List<Booking>());

        // Act
        var result = await _service.CanDeleteUserAsync("alice");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanDeleteUserAsync_WithNonZeroBalance_ShouldReturnFalse()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = "alice", Name = "Alice", IsActive = true },
            new User { Id = "bob", Name = "Bob", IsActive = true }
        };

        var bookings = new List<Booking>
        {
            new Booking("alice", "bob", 10m, "Test")
        };

        _mockDataService.Setup(x => x.GetUsersAsync()).ReturnsAsync(users);
        _mockDataService.Setup(x => x.GetBookingsAsync()).ReturnsAsync(bookings);

        // Act
        var result = await _service.CanDeleteUserAsync("alice");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ApplySettlementAsync_ShouldCreateBookingAndLog()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = "alice", Name = "Alice", IsActive = true },
            new User { Id = "bob", Name = "Bob", IsActive = true }
        };

        var settlement = new Settlement("alice", "bob", 25m)
        {
            Payer = users[0],
            Recipient = users[1]
        };

        var createdBooking = new Booking("alice", "bob", 25m, "Ausgleich");

        _mockDataService.Setup(x => x.AddBookingAsync(It.IsAny<Booking>()))
                       .Returns(Task.CompletedTask);

        // Act
        await _service.ApplySettlementAsync(settlement);

        // Assert
        _mockDataService.Verify(x => x.AddBookingAsync(It.Is<Booking>(b => 
            b.PayerId == "alice" && 
            b.BeneficiaryId == "bob" && 
            b.Amount == 25m && 
            b.Article.StartsWith("Ausgleich") &&
            b.IsSettlement)), Times.Once);

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Applying settlement")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyAllSettlementsAsync_ShouldApplyAllCalculatedSettlements()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = "alice", Name = "Alice", IsActive = true },
            new User { Id = "bob", Name = "Bob", IsActive = true }
        };

        var bookings = new List<Booking>
        {
            new Booking("bob", "alice", 20m, "Test")
        };

        _mockDataService.Setup(x => x.GetUsersAsync()).ReturnsAsync(users);
        _mockDataService.Setup(x => x.GetBookingsAsync()).ReturnsAsync(bookings);
        _mockDataService.Setup(x => x.AddBookingAsync(It.IsAny<Booking>()))
                       .Returns(Task.CompletedTask);

        // Act
        await _service.ApplyAllSettlementsAsync();

        // Assert
        _mockDataService.Verify(x => x.AddBookingAsync(It.IsAny<Booking>()), Times.Once);
        
        // Verify logging for starting and completing the operation
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting to apply all settlements")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CalculateBalancesAsync_WhenExceptionOccurs_ShouldLogErrorAndRethrow()
    {
        // Arrange
        _mockDataService.Setup(x => x.GetUsersAsync())
                       .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CalculateBalancesAsync());

        // Verify error logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred during balance calculation")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}