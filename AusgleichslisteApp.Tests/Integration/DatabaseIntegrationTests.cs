using AusgleichslisteApp.Data;
using AusgleichslisteApp.Models;
using AusgleichslisteApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AusgleichslisteApp.Tests.Integration;

public class DatabaseIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AusgleichslisteDbContext _context;
    private readonly IDataService _dataService;
    private readonly ISettlementService _settlementService;

    public DatabaseIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Add in-memory database
        services.AddDbContext<AusgleichslisteDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                   .EnableSensitiveDataLogging());
            
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add services
        services.AddScoped<IDataService, EfDataService>();
        services.AddScoped<ISettlementService, SettlementService>();

        _serviceProvider = services.BuildServiceProvider();
        
        _context = _serviceProvider.GetRequiredService<AusgleichslisteDbContext>();
        _dataService = _serviceProvider.GetRequiredService<IDataService>();
        _settlementService = _serviceProvider.GetRequiredService<ISettlementService>();
    }

    [Fact]
    public async Task FullWorkflow_CreateUsersAndBookings_ShouldWorkEndToEnd()
    {
        // Arrange & Act - Create users
        var aliceUser = new User { Name = "Alice", IsActive = true };
        var bobUser = new User { Name = "Bob", IsActive = true };
        var charlieUser = new User { Name = "Charlie", IsActive = true };
        
        await _dataService.AddUserAsync(aliceUser);
        await _dataService.AddUserAsync(bobUser);
        await _dataService.AddUserAsync(charlieUser);
        
        // Get users back from the database
        var users = await _dataService.GetUsersAsync();
        var alice = users.First(u => u.Name == "Alice");
        var bob = users.First(u => u.Name == "Bob");
        var charlie = users.First(u => u.Name == "Charlie");

        // Act - Create bookings
        await _dataService.AddBookingAsync(new Booking(alice.Id, bob.Id, 30m, "Lunch"));
        await _dataService.AddBookingAsync(new Booking(bob.Id, charlie.Id, 20m, "Coffee"));
        await _dataService.AddBookingAsync(new Booking(charlie.Id, alice.Id, 10m, "Taxi"));

        // Act - Calculate balances
        var balances = await _settlementService.CalculateBalancesAsync();

        // Assert balances
        balances.Should().HaveCount(3);
        
        var aliceBalance = balances.First(b => b.UserId == alice.Id);
        aliceBalance.NetBalance.Should().Be(20m); // +30 -10

        var bobBalance = balances.First(b => b.UserId == bob.Id);
        bobBalance.NetBalance.Should().Be(-10m); // -30 +20

        var charlieBalance = balances.First(b => b.UserId == charlie.Id);
        charlieBalance.NetBalance.Should().Be(-10m); // -20 +10

        // Act - Calculate settlements
        var settlements = await _settlementService.CalculateMinimalTransfersAsync();

        // Assert settlements
        settlements.Should().HaveCount(2);
        settlements.Sum(s => s.Amount).Should().Be(20m); // Total positive balance

        // Act - Apply settlements
        var initialBookingCount = (await _dataService.GetBookingsAsync()).Count;
        await _settlementService.ApplyAllSettlementsAsync();

        // Assert settlements were applied
        var finalBookings = await _dataService.GetBookingsAsync();
        finalBookings.Count.Should().Be(initialBookingCount + settlements.Count);

        // Verify settlement bookings
        var settlementBookings = finalBookings.Where(b => b.IsSettlement).ToList();
        settlementBookings.Should().HaveCount(settlements.Count);

        // After settlement, all balances should be zero
        var finalBalances = await _settlementService.CalculateBalancesAsync();
        finalBalances.All(b => Math.Abs(b.NetBalance) < 0.01m).Should().BeTrue();
    }

    [Fact]
    public async Task UserDeletion_WithZeroBalance_ShouldWork()
    {
        // Arrange
        var testUser = new User { Name = "TestUser", IsActive = true };
        await _dataService.AddUserAsync(testUser);
        
        // Get the user back
        var users = await _dataService.GetUsersAsync();
        var user = users.First(u => u.Name == "TestUser");

        // Act & Assert - Should be deleteable when balance is zero
        var canDelete = await _settlementService.CanDeleteUserAsync(user.Id);
        canDelete.Should().BeTrue();

        await _dataService.DeleteUserAsync(user.Id);

        var finalUsers = await _dataService.GetUsersAsync();
        finalUsers.Should().NotContain(u => u.Id == user.Id);
    }

    [Fact]
    public async Task UserDeletion_WithNonZeroBalance_ShouldNotBeAllowed()
    {
        // Arrange
        var aliceUser = new User { Name = "Alice", IsActive = true };
        await _dataService.AddUserAsync(aliceUser);
        
        var bobUser = new User { Name = "Bob", IsActive = true };
        await _dataService.AddUserAsync(bobUser);

        // Get users back from database
        var users = await _dataService.GetUsersAsync();
        var alice = users.First(u => u.Name == "Alice");
        var bob = users.First(u => u.Name == "Bob");

        await _dataService.AddBookingAsync(new Booking(alice.Id, bob.Id, 10m, "Test"));

        // Act & Assert
        var canDeleteAlice = await _settlementService.CanDeleteUserAsync(alice.Id);
        var canDeleteBob = await _settlementService.CanDeleteUserAsync(bob.Id);

        canDeleteAlice.Should().BeFalse(); // Alice has positive balance
        canDeleteBob.Should().BeFalse();   // Bob has negative balance
    }

    [Fact]
    public async Task ComplexScenario_MultipleUsersAndBookings_ShouldCalculateCorrectly()
    {
        // Arrange - Create 4 users
        var userList = new List<User>();
        for (int i = 1; i <= 4; i++)
        {
            var user = new User { Name = $"User{i}", IsActive = true };
            await _dataService.AddUserAsync(user);
            userList.Add(user);
        }

        // Get users back from database to get their IDs
        var users = await _dataService.GetUsersAsync();
        var orderedUsers = users.OrderBy(u => u.Name).ToList();

        // Create complex booking scenario
        var bookings = new[]
        {
            new { Payer = 0, Beneficiary = 1, Amount = 50m, Article = "Restaurant" },
            new { Payer = 1, Beneficiary = 2, Amount = 30m, Article = "Gas" },
            new { Payer = 2, Beneficiary = 3, Amount = 20m, Article = "Movie tickets" },
            new { Payer = 3, Beneficiary = 0, Amount = 15m, Article = "Coffee" },
            new { Payer = 0, Beneficiary = 2, Amount = 25m, Article = "Groceries" },
            new { Payer = 1, Beneficiary = 3, Amount = 10m, Article = "Parking" }
        };

        foreach (var booking in bookings)
        {
            await _dataService.AddBookingAsync(new Booking(orderedUsers[booking.Payer].Id, orderedUsers[booking.Beneficiary].Id, booking.Amount, booking.Article));
        }

        // Act
        var balances = await _settlementService.CalculateBalancesAsync();
        var debts = await _settlementService.CalculateCurrentDebtsAsync();
        var settlements = await _settlementService.CalculateMinimalTransfersAsync();

        // Assert
        balances.Should().HaveCount(4);
        
        // Total of all balances should be zero (conservation)
        balances.Sum(b => b.NetBalance).Should().Be(0m);

        // Number of settlements should be less than number of debts (optimization)
        if (debts.Count > 0)
        {
            settlements.Count.Should().BeLessThanOrEqualTo(debts.Count);
        }

        // Sum of settlement amounts should equal sum of positive balances
        var positiveBalanceSum = balances.Where(b => b.NetBalance > 0).Sum(b => b.NetBalance);
        settlements.Sum(s => s.Amount).Should().Be(positiveBalanceSum);
    }

    [Fact]
    public async Task InactiveUser_ShouldBeExcludedFromCalculations()
    {
        // Arrange
        var activeUser = new User { Name = "Active", IsActive = true };
        await _dataService.AddUserAsync(activeUser);
        
        var inactiveUser = new User { Name = "Inactive", IsActive = false };
        await _dataService.AddUserAsync(inactiveUser);

        // Get users back from database
        var users = await _dataService.GetUsersAsync();
        var active = users.First(u => u.Name == "Active");
        var inactive = users.First(u => u.Name == "Inactive");

        await _dataService.AddBookingAsync(new Booking(active.Id, inactive.Id, 10m, "Test"));

        // Act
        var balances = await _settlementService.CalculateBalancesAsync();
        var debts = await _settlementService.CalculateCurrentDebtsAsync();
        var settlements = await _settlementService.CalculateMinimalTransfersAsync();

        // Assert
        balances.Should().HaveCount(1); // Only active user
        balances.First().UserId.Should().Be(active.Id);
        balances.First().NetBalance.Should().Be(0m); // No balance since other user is inactive

        debts.Should().BeEmpty(); // No debts since one user is inactive
        settlements.Should().BeEmpty(); // No settlements needed
    }

    [Fact]
    public async Task DatabasePersistence_ShouldWorkCorrectly()
    {
        // Arrange
        var persistenceTestUser = new User { Name = "Persistence Test", IsActive = true };
        await _dataService.AddUserAsync(persistenceTestUser);
        
        // Get the user back
        var users = await _dataService.GetUsersAsync();
        var user = users.First(u => u.Name == "Persistence Test");
        var booking = new Booking(user.Id, user.Id, 100m, "Test booking");
        await _dataService.AddBookingAsync(booking);

        // Act - Retrieve data directly from database
        var dbUser = await _context.Users.FindAsync(user.Id);
        var dbBooking = await _context.Bookings.FindAsync(booking.Id);

        // Assert
        dbUser.Should().NotBeNull();
        dbUser!.Name.Should().Be("Persistence Test");
        dbUser.IsActive.Should().BeTrue();

        dbBooking.Should().NotBeNull();
        dbBooking!.Amount.Should().Be(100m);
        dbBooking.Article.Should().Be("Test booking");
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}