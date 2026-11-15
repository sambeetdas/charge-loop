using ChargeLoop.Application.DTOs;
using ChargeLoop.Infrastructure.Persistence;
using ChargeLoop.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChargeLoop.UnitTests;

public class AuthAndRoleTests
{
    private ChargeLoopDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ChargeLoopDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChargeLoopDbContext(options);
    }

    [Fact]
    public async Task SamePhoneNumber_WorksForBothDriverAndVendor_AndPreservesPastDataOnSignIn()
    {
        var context = CreateInMemoryDbContext();
        var driverService = new DriverService(context);
        var vendorService = new VendorService(context);

        // 1. First time Sign-Up as Driver (10 digits)
        var dSignUp = await driverService.AuthenticateWithPhoneAsync(new AuthRequest("9876543210", "Ravi Kumar", "en"));
        Assert.Equal("+919876543210", dSignUp.PhoneNumber);
        Assert.Equal(0.00m, dSignUp.WalletBalance);

        // 2. First time Sign-Up as Partner with the SAME phone number
        var vSignUp = await vendorService.AuthenticateWithPhoneAsync(new VendorAuthRequest("9876543210", "Ravi Kumar", "Ravi Swap Hub"));
        Assert.Equal("+919876543210", vSignUp.PhoneNumber);
        Assert.NotEqual(dSignUp.DriverId, vSignUp.VendorId);

        // 3. Second time Sign-In as Driver using +91 format -> returns SAME DriverId & past data
        var dSignIn = await driverService.AuthenticateWithPhoneAsync(new AuthRequest("+919876543210", null, "en"));
        Assert.Equal(dSignUp.DriverId, dSignIn.DriverId);
        Assert.Equal("Ravi Kumar", dSignIn.FullName);
        Assert.Equal(0.00m, dSignIn.WalletBalance);

        // 4. Second time Sign-In as Partner using +91 format -> returns SAME VendorId & past data
        var vSignIn = await vendorService.AuthenticateWithPhoneAsync(new VendorAuthRequest("+919876543210", null, null));
        Assert.Equal(vSignUp.VendorId, vSignIn.VendorId);
        Assert.Equal("Ravi Swap Hub", vSignIn.BusinessName);
    }
}
