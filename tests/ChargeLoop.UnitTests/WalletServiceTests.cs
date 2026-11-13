using ChargeLoop.Application.DTOs;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Domain.Enums;
using ChargeLoop.Infrastructure.Persistence;
using ChargeLoop.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChargeLoop.UnitTests;

public class WalletServiceTests
{
    private ChargeLoopDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ChargeLoopDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChargeLoopDbContext(options);
    }

    [Fact]
    public async Task TopUpAsync_CreditsDriverWalletAndCreatesTransactionRecord()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var walletService = new WalletService(context);
        var driver = new Driver { Id = Guid.NewGuid(), PhoneNumber = "999", FullName = "Driver 1", WalletBalance = 50.00m };
        await context.Drivers.AddAsync(driver);
        await context.SaveChangesAsync();

        var request = new WalletTopUpRequest(
            AccountId: driver.Id,
            AccountType: WalletAccountType.Driver,
            Amount: 200.00m,
            PaymentReference: "UPI-TEST-12345"
        );

        // Act
        var result = await walletService.TopUpAsync(request);

        // Assert
        Assert.Equal(250.00m, result.Balance);
        var updated = await context.Drivers.FindAsync(driver.Id);
        Assert.Equal(250.00m, updated!.WalletBalance);

        var tx = await context.WalletTransactions.FirstOrDefaultAsync(t => t.AccountId == driver.Id);
        Assert.NotNull(tx);
        Assert.Equal(200.00m, tx.Amount);
        Assert.Equal(WalletTransactionType.Credit, tx.Type);
    }

    [Fact]
    public async Task DeductDriverAmountAsync_ReturnsFalseWhenBalanceInsufficient()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var walletService = new WalletService(context);
        var driver = new Driver { Id = Guid.NewGuid(), PhoneNumber = "999", FullName = "Driver 1", WalletBalance = 50.00m };
        await context.Drivers.AddAsync(driver);
        await context.SaveChangesAsync();

        // Act (Attempting to deduct 118 when balance is 50)
        var success = await walletService.DeductDriverAmountAsync(driver.Id, 118.00m, "Swap Fee", Guid.NewGuid());

        // Assert
        Assert.False(success);
        var updated = await context.Drivers.FindAsync(driver.Id);
        Assert.Equal(50.00m, updated!.WalletBalance); // Balance untouched
    }

    [Fact]
    public async Task CreditPartnerCommissionAsync_CreditsPartnerBalanceInstantly()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var walletService = new WalletService(context);
        var partner = new Partner { Id = Guid.NewGuid(), FullName = "Shop Owner", BusinessName = "Auto Care", PhoneNumber = "111", WalletBalance = 1000.00m };
        await context.Partners.AddAsync(partner);
        await context.SaveChangesAsync();

        // Act
        var success = await walletService.CreditPartnerCommissionAsync(partner.Id, 25.00m, Guid.NewGuid());

        // Assert
        Assert.True(success);
        var updated = await context.Partners.FindAsync(partner.Id);
        Assert.Equal(1025.00m, updated!.WalletBalance);
    }
}
