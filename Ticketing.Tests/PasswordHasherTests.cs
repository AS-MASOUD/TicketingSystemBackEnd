using Ticketing.Application.Security;
using Xunit;

namespace Ticketing.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_ShouldReturnValidHashString()
    {
        // Arrange
        string password = "TestPassword@123";

        // Act
        string hash = SecurePasswordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        var parts = hash.Split(':');
        Assert.Equal(4, parts.Length); // salt:hash:iterations:algorithm
        Assert.Equal("100000", parts[2]);
        Assert.Equal("SHA256", parts[3]);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        string password = "StrongPassword#2026";
        string hash = SecurePasswordHasher.HashPassword(password);

        // Act
        bool result = SecurePasswordHasher.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        string password = "CorrectPassword123";
        string wrongPassword = "WrongPassword123";
        string hash = SecurePasswordHasher.HashPassword(password);

        // Act
        bool result = SecurePasswordHasher.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_WithNullOrEmpty_ShouldReturnFalse()
    {
        // Arrange
        string hash = SecurePasswordHasher.HashPassword("ValidPassword");

        // Act & Assert
        Assert.False(SecurePasswordHasher.VerifyPassword("", hash));
        Assert.False(SecurePasswordHasher.VerifyPassword("ValidPassword", ""));
        Assert.False(SecurePasswordHasher.VerifyPassword("ValidPassword", "invalid:hash:format"));
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_ShouldProduceDifferentSalts()
    {
        // Arrange
        string password = "IdenticalPassword";

        // Act
        string hash1 = SecurePasswordHasher.HashPassword(password);
        string hash2 = SecurePasswordHasher.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2);
        Assert.True(SecurePasswordHasher.VerifyPassword(password, hash1));
        Assert.True(SecurePasswordHasher.VerifyPassword(password, hash2));
    }
}
