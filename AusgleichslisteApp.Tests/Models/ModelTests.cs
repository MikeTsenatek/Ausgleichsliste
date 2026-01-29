using AusgleichslisteApp.Models;

namespace AusgleichslisteApp.Tests.Models;

public class ModelTests
{
    [Fact]
    public void User_Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var user = new User();

        // Assert
        user.Id.Should().NotBeNullOrEmpty();
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void User_WithName_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var name = "Test User";

        // Act
        var user = new User { Name = name };

        // Assert
        user.Name.Should().Be(name);
        user.Id.Should().NotBeNullOrEmpty();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Booking_Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var booking = new Booking();

        // Assert
        booking.Id.Should().NotBe(Guid.Empty);
        booking.Date.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
        booking.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Booking_WithAllProperties_ShouldSetCorrectly()
    {
        // Arrange
        var amount = 25.50m;
        var article = "Test Booking";
        var date = DateTime.Today.AddDays(-1);

        // Act
        var booking = new Booking("payer-123", "beneficiary-456", amount, article, date);

        // Assert
        booking.PayerId.Should().Be("payer-123");
        booking.BeneficiaryId.Should().Be("beneficiary-456");
        booking.Amount.Should().Be(amount);
        booking.Article.Should().Be(article);
        booking.Date.Should().Be(date);
    }

    [Fact]
    public void Balance_Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = "user-123";
        var netBalance = 42.75m;

        // Act
        var balance = new Balance(userId, netBalance);

        // Assert
        balance.UserId.Should().Be(userId);
        balance.NetBalance.Should().Be(netBalance);
    }

    [Fact]
    public void Settlement_Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var payerId = "payer-123";
        var recipientId = "recipient-456";
        var amount = 30.25m;

        // Act
        var settlement = new Settlement(payerId, recipientId, amount);

        // Assert
        settlement.PayerId.Should().Be(payerId);
        settlement.RecipientId.Should().Be(recipientId);
        settlement.Amount.Should().Be(amount);
    }

    [Fact]
    public void Settlement_ToBooking_ShouldCreateCorrectBooking()
    {
        // Arrange
        var settlement = new Settlement("alice", "bob", 15.50m);
        var article = "Test Settlement";

        // Act
        var booking = settlement.ToBooking(article);

        // Assert
        booking.PayerId.Should().Be("alice");
        booking.BeneficiaryId.Should().Be("bob");
        booking.Amount.Should().Be(15.50m);
        booking.Article.Should().Be("Test Settlement alice->bob");
        booking.Date.Should().BeCloseTo(DateTime.Today, TimeSpan.FromDays(1));
        booking.Id.Should().NotBe(Guid.Empty);
        booking.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Debt_Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var debtorId = "debtor-123";
        var creditorId = "creditor-456";
        var amount = 18.75m;

        // Act
        var debt = new Debt(debtorId, creditorId, amount);

        // Assert
        debt.DebtorId.Should().Be(debtorId);
        debt.CreditorId.Should().Be(creditorId);
        debt.Amount.Should().Be(amount);
    }

    [Fact]
    public void Models_ShouldHandleNullValues_Gracefully()
    {
        // Arrange & Act
        var user = new User { Name = null! };
        var booking = new Booking { Article = null! };

        // Assert - Should not throw exceptions
        user.Name.Should().BeNull();
        booking.Article.Should().BeNull();
        
        // Other properties should still work
        user.Id.Should().NotBeNullOrEmpty();
        booking.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Models_WithNavigationProperties_ShouldAllowAssignment()
    {
        // Arrange
        var payer = new User { Id = "payer", Name = "Payer" };
        var beneficiary = new User { Id = "beneficiary", Name = "Beneficiary" };
        
        var booking = new Booking
        {
            PayerId = payer.Id,
            BeneficiaryId = beneficiary.Id,
            Amount = 10m
        };

        var balance = new Balance("user", 5m) { User = payer };
        var settlement = new Settlement("payer", "recipient", 7.5m) { Payer = payer, Recipient = beneficiary };
        var debt = new Debt("debtor", "creditor", 12.5m) { Debtor = payer, Creditor = beneficiary };

        // Act & Assert - Should not throw and properties should be set
        booking.Payer = payer;
        booking.Beneficiary = beneficiary;

        booking.Payer.Should().Be(payer);
        booking.Beneficiary.Should().Be(beneficiary);
        balance.User.Should().Be(payer);
        settlement.Payer.Should().Be(payer);
        settlement.Recipient.Should().Be(beneficiary);
        debt.Debtor.Should().Be(payer);
        debt.Creditor.Should().Be(beneficiary);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(1.0)]
    [InlineData(999.99)]
    [InlineData(1000000.50)]
    public void Booking_WithVariousAmounts_ShouldHandleCorrectly(decimal amount)
    {
        // Act
        var booking = new Booking { Amount = amount };

        // Assert
        booking.Amount.Should().Be(amount);
    }

    [Theory]
    [InlineData(-100.0)]
    [InlineData(-0.01)]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(100.0)]
    public void Balance_WithVariousNetBalances_ShouldHandleCorrectly(decimal netBalance)
    {
        // Act
        var balance = new Balance("user", netBalance);

        // Assert
        balance.NetBalance.Should().Be(netBalance);
    }

    [Fact]
    public void Models_DateProperties_ShouldHandleDifferentDates()
    {
        // Arrange
        var pastDate = DateTime.Today.AddDays(-30);
        var futureDate = DateTime.Today.AddDays(30);

        // Act
        var booking1 = new Booking { Date = pastDate };
        var booking2 = new Booking { Date = futureDate };

        // Assert
        booking1.Date.Should().Be(pastDate);
        booking2.Date.Should().Be(futureDate);
    }
}