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
    public async Task DriverAndVendorProfile_UpdateAndChangePhoneWithPin()
    {
        var context = CreateInMemoryDbContext();
        var driverService = new DriverService(context);
        var vendorService = new VendorService(context);

        // Driver profile edit & phone change with PIN
        var dAuth = await driverService.SignUpWithPinAsync(new DriverPinSignUpRequest("9111111111", "1234", "1234", "Old Driver"));
        var dUpdated = await driverService.UpdateProfileAsync(dAuth.DriverId, new UpdateDriverProfileRequest("New Driver", "driver@test.com", "hi"));
        Assert.NotNull(dUpdated);
        Assert.Equal("New Driver", dUpdated!.FullName);
        Assert.Equal("driver@test.com", dUpdated.Email);
        Assert.Equal("hi", dUpdated.LanguageCode);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            driverService.ChangePhoneAsync(dAuth.DriverId, new ChangePhoneRequest("9222222222", "0000")));

        var dPhoneChanged = await driverService.ChangePhoneAsync(dAuth.DriverId, new ChangePhoneRequest("9222222222", "1234"));
        Assert.Equal("+919222222222", dPhoneChanged.PhoneNumber);

        // Vendor profile edit & phone change with PIN
        var vAuth = await vendorService.SignUpWithPinAsync(new VendorPinSignUpRequest("9333333333", "4321", "4321", "Old Vendor", "Old Biz"));
        var vUpdated = await vendorService.UpdateProfileAsync(vAuth.VendorId,
            new UpdateVendorProfileRequest("New Vendor", "XYZ Mall Hub", "vendor@test.com", "vendor@upi", "1234567890"));
        Assert.NotNull(vUpdated);
        Assert.Equal("New Vendor", vUpdated!.FullName);
        Assert.Equal("XYZ Mall Hub", vUpdated.BusinessName);
        Assert.Equal("vendor@test.com", vUpdated.Email);
        Assert.Equal("vendor@upi", vUpdated.UpiId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            vendorService.ChangePhoneAsync(vAuth.VendorId, new ChangePhoneRequest("9444444444", "0000")));

        var vPhoneChanged = await vendorService.ChangePhoneAsync(vAuth.VendorId, new ChangePhoneRequest("9444444444", "4321"));
        Assert.Equal("+919444444444", vPhoneChanged.PhoneNumber);
    }

    [Fact]
    public async Task PinAuth_DriverAndVendor_SignUpSignInAndResetPin()
    {
        var context = CreateInMemoryDbContext();
        var driverService = new DriverService(context);
        var vendorService = new VendorService(context);

        // Driver Sign Up -> Sign In -> Reset PIN -> Sign In with New PIN
        await Assert.ThrowsAsync<ArgumentException>(() =>
            driverService.SignUpWithPinAsync(new DriverPinSignUpRequest("9555555555", "1234", "9999")));

        var dUp = await driverService.SignUpWithPinAsync(new DriverPinSignUpRequest("9555555555", "1234", "1234", "Pin Driver"));
        Assert.Equal("+919555555555", dUp.PhoneNumber);
        Assert.Equal(0.00m, dUp.WalletBalance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            driverService.SignInWithPinAsync(new DriverPinSignInRequest("9555555555", "0000")));

        var dIn = await driverService.SignInWithPinAsync(new DriverPinSignInRequest("+919555555555", "1234"));
        Assert.Equal(dUp.DriverId, dIn.DriverId);

        // Without security question set -> blocked with Call/Email support message
        var noSqEx = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            driverService.ResetPinAsync(new ResetPinRequest("9555555555", "5678", "5678")));
        Assert.Contains("+91 95914 71658", noSqEx.Message);

        // Set security question in Profile -> wrong answer rejected, right answer resets PIN
        await driverService.UpdateProfileAsync(dUp.DriverId, new UpdateDriverProfileRequest(null, null, null, "What is your birth city?", "Bengaluru"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            driverService.ResetPinAsync(new ResetPinRequest("9555555555", "5678", "5678", "wrong")));
        await driverService.ResetPinAsync(new ResetPinRequest("9555555555", "5678", "5678", " bengaluru "));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            driverService.SignInWithPinAsync(new DriverPinSignInRequest("9555555555", "1234")));
        var dInNew = await driverService.SignInWithPinAsync(new DriverPinSignInRequest("9555555555", "5678"));
        Assert.Equal(dUp.DriverId, dInNew.DriverId);

        // Vendor Sign Up with Security Question -> Sign In -> Reset PIN -> Sign In with New PIN
        var vUp = await vendorService.SignUpWithPinAsync(new VendorPinSignUpRequest(
            "9666666666", "4321", "4321", "Pin Vendor", "Pin Hub", null, "What was your first vehicle model?", "Ather 450X"));
        Assert.Equal("+919666666666", vUp.PhoneNumber);
        Assert.Equal(0.00m, vUp.WalletBalance);

        var vIn = await vendorService.SignInWithPinAsync(new VendorPinSignInRequest("+919666666666", "4321"));
        Assert.Equal(vUp.VendorId, vIn.VendorId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            vendorService.ResetPinAsync(new ResetPinRequest("9666666666", "8765", "8765", "wrong")));
        await vendorService.ResetPinAsync(new ResetPinRequest("9666666666", "8765", "8765", "ather 450x"));
        var vInNew = await vendorService.SignInWithPinAsync(new VendorPinSignInRequest("9666666666", "8765"));
        Assert.Equal(vUp.VendorId, vInNew.VendorId);
    }
}
