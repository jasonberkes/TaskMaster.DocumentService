using TaskMaster.DocumentService.Core.Authentication;

namespace TaskMaster.DocumentService.Core.Tests.Authentication;

/// <summary>
/// Unit tests for ApiKeyHasher.
/// </summary>
public class ApiKeyHasherTests
{
    [Fact]
    public void HashApiKey_ProducesConsistentHash()
    {
        // Arrange
        var apiKey = "test-api-key-12345";

        // Act
        var hash1 = ApiKeyHasher.HashApiKey(apiKey);
        var hash2 = ApiKeyHasher.HashApiKey(apiKey);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashApiKey_DifferentKeys_ProduceDifferentHashes()
    {
        // Arrange
        var apiKey1 = "test-api-key-1";
        var apiKey2 = "test-api-key-2";

        // Act
        var hash1 = ApiKeyHasher.HashApiKey(apiKey1);
        var hash2 = ApiKeyHasher.HashApiKey(apiKey2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashApiKey_ReturnsBase64String()
    {
        // Arrange
        var apiKey = "test-api-key";

        // Act
        var hash = ApiKeyHasher.HashApiKey(apiKey);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        // Verify it's valid base64
        var bytes = Convert.FromBase64String(hash);
        Assert.NotEmpty(bytes);
    }
}
