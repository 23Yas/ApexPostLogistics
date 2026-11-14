namespace ApexPost.Tests;

using ApexPost.Api.Filters;

public sealed class PartnerApiKeyFilterTests
{
    private static readonly string[] Keys = { "test-partner-key-123", "second-key" };

    [Fact]
    public void ValidKey_IsAccepted()
    {
        Assert.True(PartnerApiKeyFilter.IsValidKey("test-partner-key-123", Keys));
        Assert.True(PartnerApiKeyFilter.IsValidKey("second-key", Keys));
    }

    [Fact]
    public void WrongOrMissingKey_IsRejected()
    {
        Assert.False(PartnerApiKeyFilter.IsValidKey("wrong-key", Keys));
        Assert.False(PartnerApiKeyFilter.IsValidKey("", Keys));
        Assert.False(PartnerApiKeyFilter.IsValidKey("test-partner-key-123", Array.Empty<string>()));
    }

    [Fact]
    public void Sha256HexHashConfig_IsAccepted()
    {
        // 64-char hex values are treated as pre-computed SHA-256 hashes.
        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("hashed-key"))).ToLowerInvariant();

        Assert.True(PartnerApiKeyFilter.IsValidKey("hashed-key", new[] { hash }));

        var otherHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("other-key"))).ToLowerInvariant();
        Assert.False(PartnerApiKeyFilter.IsValidKey("hashed-key", new[] { otherHash }));
    }
}
