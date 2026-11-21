using System.Security.Cryptography;

namespace ChargeLoop.Infrastructure.Services;

internal static class PinHasher
{
    public static void ValidatePinPair(string pin, string confirmPin)
    {
        var p = (pin ?? string.Empty).Trim();
        var c = (confirmPin ?? string.Empty).Trim();
        if (p.Length < 4 || p.Length > 6 || !p.All(char.IsDigit))
            throw new ArgumentException("PIN must be 4 to 6 digits.");
        if (!string.Equals(p, c, StringComparison.Ordinal))
            throw new ArgumentException("PIN and Confirm PIN do not match.");
    }

    public static string Hash(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(pin.Trim(), salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string pin, string? storedHash)
    {
        if (string.IsNullOrWhiteSpace(pin) || string.IsNullOrWhiteSpace(storedHash))
            return false;
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(pin.Trim(), salt, 100_000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}
