using System.Security.Cryptography;

namespace Ticketing.Application.Security;

public static class SecurePasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
    private const char SegmentDelimiter = ':';

    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            Algorithm,
            KeySize
        );

        return string.Join(
            SegmentDelimiter,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash),
            Iterations,
            Algorithm.Name
        );
    }

    public static bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrEmpty(password))
            return false;

        string[] segments = hashedPassword.Split(SegmentDelimiter);
        if (segments.Length != 4)
            return false;

        try
        {
            byte[] salt = Convert.FromBase64String(segments[0]);
            byte[] expectedHash = Convert.FromBase64String(segments[1]);
            if (!int.TryParse(segments[2], out int iterations))
                return false;

            var algorithm = new HashAlgorithmName(segments[3]);

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                algorithm,
                expectedHash.Length
            );

            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch
        {
            return false;
        }
    }
}
