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
        Assert.Equal(0.00m, vSignIn.WalletBalance);
    }

    [Fact]
    public async Task VendorOtpFlow_SendsAndVerifiesOtp_AndStartsWithZeroWallet()
    {
        var context = CreateInMemoryDbContext();
        var driverService = new DriverService(context);
        var vendorService = new VendorService(context);

        var otpResp = await driverService.SendOtpAsync(new SendOtpRequest("9988776655"));
        Assert.Equal("+919988776655", otpResp.PhoneNumber);
        Assert.Equal(6, otpResp.DemoOtp.Length);

        var vendorAuth = await vendorService.VerifyOtpAndAuthenticateAsync(
            new VerifyVendorOtpRequest("9988776655", otpResp.DemoOtp, "Anita Partner", "Anita Hub"));

        Assert.Equal("+919988776655", vendorAuth.PhoneNumber);
        Assert.Equal("Anita Partner", vendorAuth.FullName);
        Assert.Equal("Anita Hub", vendorAuth.BusinessName);
        Assert.Equal(0.00m, vendorAuth.WalletBalance);
    }

    [Fact]
    public async Task DriverAndVendorProfile_UpdateAndChangePhoneWithOtp()
    {
        var context = CreateInMemoryDbContext();
        var driverService = new DriverService(context);
        var vendorService = new VendorService(context);

        // Driver profile edit & phone change
        var dAuth = await driverService.AuthenticateWithPhoneAsync(new AuthRequest("9111111111", "Old Driver", "en"));
        var dUpdated = await driverService.UpdateProfileAsync(dAuth.DriverId, new UpdateDriverProfileRequest("New Driver", "driver@test.com", "hi"));
        Assert.NotNull(dUpdated);
        Assert.Equal("New Driver", dUpdated!.FullName);
        Assert.Equal("driver@test.com", dUpdated.Email);
        Assert.Equal("hi", dUpdated.LanguageCode);

        var dOtp = await driverService.SendOtpAsync(new SendOtpRequest("9222222222"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            driverService.ChangePhoneAsync(dAuth.DriverId, new ChangePhoneRequest("9222222222", "000000")));

        var dPhoneChanged = await driverService.ChangePhoneAsync(dAuth.DriverId, new ChangePhoneRequest("9222222222", dOtp.DemoOtp));
        Assert.Equal("+919222222222", dPhoneChanged.PhoneNumber);

        // Vendor profile edit & phone change
        var vAuth = await vendorService.AuthenticateWithPhoneAsync(new VendorAuthRequest("9333333333", "Old Vendor", "Old Biz"));
        var vUpdated = await vendorService.UpdateProfileAsync(vAuth.VendorId,
            new UpdateVendorProfileRequest("New Vendor", "XYZ Mall Hub", "vendor@test.com", "vendor@upi", "1234567890"));
        Assert.NotNull(vUpdated);
        Assert.Equal("New Vendor", vUpdated!.FullName);
        Assert.Equal("XYZ Mall Hub", vUpdated.BusinessName);
        Assert.Equal("vendor@test.com", vUpdated.Email);
        Assert.Equal("vendor@upi", vUpdated.UpiId);

        var vOtp = await driverService.SendOtpAsync(new SendOtpRequest("9444444444"));
        var vPhoneChanged = await vendorService.ChangePhoneAsync(vAuth.VendorId, new ChangePhoneRequest("9444444444", vOtp.DemoOtp));
        Assert.Equal("+919444444444", vPhoneChanged.PhoneNumber);
    }
}
