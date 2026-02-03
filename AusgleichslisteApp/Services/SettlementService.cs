using AusgleichslisteApp.Models;

namespace AusgleichslisteApp.Services
{
    /// <summary>
    /// Service für Ausgleichsberechnungen - portiert die Python-Logik
    /// </summary>
    public interface ISettlementService
    {
        Task<List<Balance>> CalculateBalancesAsync();
        Task<List<Settlement>> CalculateMinimalTransfersAsync();
        Task<List<Settlement>> GetStoredSettlementsAsync();
        Task SaveCalculatedSettlementsAsync();
        Task<List<Debt>> CalculateCurrentDebtsAsync();
        Task ApplySettlementAsync(Settlement settlement);
        Task ApplyAllSettlementsAsync();
        Task<decimal> GetUserBalanceAsync(string userId);
        Task<bool> CanDeleteUserAsync(string userId);
    }

    public class SettlementService : ISettlementService
    {
        private readonly IDataService _dataService;
        private readonly ILogger<SettlementService> _logger;

        public SettlementService(IDataService dataService, ILogger<SettlementService> logger)
        {
            _dataService = dataService;
            _logger = logger;
        }

        public async Task<List<Balance>> CalculateBalancesAsync()
        {
            _logger.LogDebug("Starting balance calculation");

            try
            {
                var users = await _dataService.GetUsersAsync();
                var bookings = await _dataService.GetBookingsAsync();

                _logger.LogInformation("Calculating balances for {UserCount} users based on {BookingCount} bookings",
                    users.Count, bookings.Count);

                // Berechne Netto-Salden pro Benutzer
                var balances = new Dictionary<string, decimal>();

                // Initialisiere alle aktiven Benutzer mit 0
                foreach (var user in users.Where(u => u.IsActive))
                {
                    balances[user.Id] = 0m;
                }

                // Addiere alle Buchungen
                foreach (var booking in bookings)
                {
                    if (!balances.ContainsKey(booking.PayerId) || !balances.ContainsKey(booking.BeneficiaryId))
                    {
                        _logger.LogWarning("Skipping booking {BookingId} - inactive user involved (Payer: {PayerId}, Beneficiary: {BeneficiaryId})",
                            booking.Id, booking.PayerId, booking.BeneficiaryId);
                        continue;
                    }

                    // Der Zahler (Payer) hat weniger Schulden -> +Amount
                    // Der Begünstigte (Beneficiary) hat mehr Schulden -> -Amount
                    balances[booking.PayerId] += booking.Amount;
                    balances[booking.BeneficiaryId] -= booking.Amount;
                }

                var result = new List<Balance>();
                foreach (var kvp in balances)
                {
                    var user = users.First(u => u.Id == kvp.Key);
                    result.Add(new Balance(kvp.Key, kvp.Value) { User = user });
                }

                var finalResult = result.OrderByDescending(b => b.NetBalance).ToList();

                _logger.LogInformation("Balance calculation completed. Found {BalanceCount} balances", finalResult.Count);
                _logger.LogDebug("Balance summary: Positive balances: {PositiveCount}, Negative balances: {NegativeCount}",
                    finalResult.Count(b => b.NetBalance > 0), finalResult.Count(b => b.NetBalance < 0));

                return finalResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during balance calculation");
                throw;
            }
        }

        public async Task<List<Settlement>> CalculateMinimalTransfersAsync()
        {
            _logger.LogDebug("Starting minimal transfers calculation");

            try
            {
                var balances = await CalculateBalancesAsync();

                // Portierung des Python-Algorithmus settle_min_transfers
                var creditors = balances
                    .Where(b => b.NetBalance > 0)
                    .Select(b => new { User = b.UserId, Amount = b.NetBalance, UserObj = b.User })
                    .OrderByDescending(x => x.Amount)
                    .ToList();

                var debtors = balances
                    .Where(b => b.NetBalance < 0)
                    .Select(b => new { User = b.UserId, Amount = -b.NetBalance, UserObj = b.User })
                    .OrderByDescending(x => x.Amount)
                    .ToList();

                _logger.LogInformation("Found {CreditorCount} creditors and {DebtorCount} debtors for settlement calculation",
                    creditors.Count, debtors.Count);

                var settlements = new List<Settlement>();
                int i = 0, j = 0;

                // Greedy-Algorithmus für minimale Transfers
                while (i < debtors.Count && j < creditors.Count)
                {
                    var debtor = debtors[i];
                    var creditor = creditors[j];

                    var payAmount = Math.Min(debtor.Amount, creditor.Amount);
                    if (payAmount > 0)
                    {
                        _logger.LogDebug("Creating settlement: {DebtorId} pays {PayAmount:F2} to {CreditorId}",
                            debtor.User, payAmount, creditor.User);

                        settlements.Add(new Settlement(debtor.User, creditor.User, payAmount)
                        {
                            Payer = debtor.UserObj,
                            Recipient = creditor.UserObj
                        });
                    }

                    // Aktualisiere Beträge
                    var newDebtorAmount = debtor.Amount - payAmount;
                    var newCreditorAmount = creditor.Amount - payAmount;

                    debtors[i] = new { debtor.User, Amount = newDebtorAmount, debtor.UserObj };
                    creditors[j] = new { creditor.User, Amount = newCreditorAmount, creditor.UserObj };

                    if (newDebtorAmount == 0) i++;
                    if (newCreditorAmount == 0) j++;
                }

                _logger.LogInformation("Minimal transfers calculation completed. Generated {SettlementCount} settlements",
                    settlements.Count);

                return settlements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during minimal transfers calculation");
                throw;
            }
        }

        public async Task<List<Debt>> CalculateCurrentDebtsAsync()
        {
            _logger.LogDebug("Starting current debts calculation");

            try
            {
                var users = await _dataService.GetUsersAsync();
                var bookings = await _dataService.GetBookingsAsync();
                var userMap = users.ToDictionary(u => u.Id, u => u);

                _logger.LogInformation("Calculating debts between {UserCount} users based on {BookingCount} bookings",
                    users.Count, bookings.Count);

                // Berechne direkte Schulden zwischen allen Paaren
                var debtMatrix = new Dictionary<(string, string), decimal>();

                foreach (var booking in bookings)
                {
                    if (!userMap.ContainsKey(booking.PayerId) || !userMap.ContainsKey(booking.BeneficiaryId))
                    {
                        _logger.LogWarning("Skipping booking {BookingId} - user not found (Payer: {PayerId}, Beneficiary: {BeneficiaryId})",
                            booking.Id, booking.PayerId, booking.BeneficiaryId);
                        continue;
                    }

                    // Filter out inactive users
                    if (!userMap[booking.PayerId].IsActive || !userMap[booking.BeneficiaryId].IsActive)
                    {
                        _logger.LogWarning("Skipping booking {BookingId} - inactive user involved (Payer: {PayerId}, Beneficiary: {BeneficiaryId})",
                            booking.Id, booking.PayerId, booking.BeneficiaryId);
                        continue;
                    }

                    if (booking.PayerId == booking.BeneficiaryId)
                    {
                        _logger.LogDebug("Skipping self-booking {BookingId} for user {UserId}", booking.Id, booking.PayerId);
                        continue;
                    }

                    var key = (booking.BeneficiaryId, booking.PayerId); // Beneficiary schuldet Payer
                    debtMatrix[key] = debtMatrix.GetValueOrDefault(key, 0) + booking.Amount;
                }

                var result = new List<Debt>();
                foreach (var kvp in debtMatrix.Where(d => d.Value > 0))
                {
                    result.Add(new Debt(kvp.Key.Item1, kvp.Key.Item2, kvp.Value)
                    {
                        Debtor = userMap[kvp.Key.Item1],
                        Creditor = userMap[kvp.Key.Item2]
                    });
                }

                var finalResult = result.OrderByDescending(d => d.Amount).ToList();

                _logger.LogInformation("Current debts calculation completed. Found {DebtCount} debts", finalResult.Count);

                return finalResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during current debts calculation");
                throw;
            }
        }

        public async Task ApplySettlementAsync(Settlement settlement)
        {
            _logger.LogInformation("Applying settlement: {PayerId} pays {Amount:F2} to {RecipientId}",
                settlement.PayerId, settlement.Amount, settlement.RecipientId);

            try
            {
                var booking = settlement.ToBooking("Ausgleich");
                await _dataService.AddBookingAsync(booking);

                // Finde das entsprechende gespeicherte Settlement und behandle es
                var storedSettlements = await _dataService.GetActiveSettlementsAsync();
                var matchingSettlement = storedSettlements.FirstOrDefault(s => 
                    s.PayerId == settlement.PayerId && s.RecipientId == settlement.RecipientId);

                if (matchingSettlement != null)
                {
                    if (settlement.Amount >= matchingSettlement.Amount)
                    {
                        // Exakte oder Überzahlung -> Settlement komplett entfernen
                        await _dataService.DeleteSettlementAsync(matchingSettlement.Id);
                        _logger.LogDebug("Removed settlement {SettlementId} - fully paid", matchingSettlement.Id);
                    }
                    else
                    {
                        // Teilzahlung -> Betrag reduzieren
                        var remainingAmount = matchingSettlement.Amount - settlement.Amount;
                        await _dataService.UpdateSettlementAmountAsync(matchingSettlement.Id, remainingAmount);
                        _logger.LogDebug("Reduced settlement {SettlementId} amount to {RemainingAmount:F2}", 
                            matchingSettlement.Id, remainingAmount);
                    }
                }

                _logger.LogDebug("Settlement successfully applied as booking {BookingId}", booking.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply settlement: {PayerId} -> {RecipientId}, Amount: {Amount:F2}",
                    settlement.PayerId, settlement.RecipientId, settlement.Amount);
                throw;
            }
        }

        public async Task ApplyAllSettlementsAsync()
        {
            _logger.LogInformation("Starting to apply all settlements");

            try
            {
                var settlements = await CalculateMinimalTransfersAsync();

                _logger.LogInformation("Found {SettlementCount} settlements to apply", settlements.Count);

                foreach (var settlement in settlements)
                {
                    await ApplySettlementAsync(settlement);
                }

                _logger.LogInformation("Successfully applied all {SettlementCount} settlements", settlements.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply all settlements");
                throw;
            }
        }

        public async Task<decimal> GetUserBalanceAsync(string userId)
        {
            _logger.LogDebug("Getting balance for user {UserId}", userId);

            try
            {
                var balances = await CalculateBalancesAsync();
                var balance = balances.FirstOrDefault(b => b.UserId == userId)?.NetBalance ?? 0m;

                _logger.LogDebug("User {UserId} balance: {Balance:F2}", userId, balance);

                return balance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get balance for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> CanDeleteUserAsync(string userId)
        {
            _logger.LogDebug("Checking if user {UserId} can be deleted", userId);

            try
            {
                var balance = await GetUserBalanceAsync(userId);
                var canDelete = Math.Abs(balance) < 0.01m; // Praktisch 0 (Rundungstolerance)

                _logger.LogInformation("User {UserId} deletion check: Balance = {Balance:F2}, CanDelete = {CanDelete}",
                    userId, balance, canDelete);

                return canDelete;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if user {UserId} can be deleted", userId);
                throw;
            }
        }

        public async Task<List<Settlement>> GetStoredSettlementsAsync()
        {
            _logger.LogDebug("Getting stored settlements from database");

            try
            {
                return await _dataService.GetActiveSettlementsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get stored settlements");
                throw;
            }
        }

        public async Task SaveCalculatedSettlementsAsync()
        {
            _logger.LogDebug("Calculating and saving new settlements to database");

            try
            {
                // Lösche alle alten Settlements
                await _dataService.ClearAllSettlementsAsync();

                // Berechne neue Settlements
                var newSettlements = await CalculateMinimalTransfersAsync();

                // Speichere neue Settlements in der Datenbank
                if (newSettlements.Any())
                {
                    await _dataService.SaveSettlementsAsync(newSettlements);
                    _logger.LogInformation("Saved {Count} new settlements to database", newSettlements.Count);
                }
                else
                {
                    _logger.LogInformation("No settlements needed - all balances are settled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save calculated settlements");
                throw;
            }
        }
    }
}