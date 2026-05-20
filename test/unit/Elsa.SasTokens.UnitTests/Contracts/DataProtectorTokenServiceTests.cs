using System.Text.Json;
using Elsa.SasTokens.Contracts;
using Microsoft.AspNetCore.DataProtection;

namespace Elsa.SasTokens.UnitTests.Contracts;

public class DataProtectorTokenServiceTests : IDisposable
{
    private readonly string _keyDirectory = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly DataProtectorTokenService _service;

    public DataProtectorTokenServiceTests()
    {
        Directory.CreateDirectory(_keyDirectory);
        _dataProtectionProvider = DataProtectionProvider.Create(new DirectoryInfo(_keyDirectory));
        _service = new DataProtectorTokenService(_dataProtectionProvider);
    }

    [Fact]
    public void DecryptToken_CreatedWithLifetimeBeforeExpiration_ReturnsPayload()
    {
        var payload = new TokenPayload("workflow-instance-1", "bookmark-1");

        var token = _service.CreateToken(payload, TimeSpan.FromMinutes(5));
        var result = _service.DecryptToken<TokenPayload>(token);

        Assert.StartsWith("v1.tl.", token);
        AssertPayload(payload, result);
    }

    [Fact]
    public void TryDecryptToken_CreatedWithPastExpiration_ReturnsFalse()
    {
        var payload = new TokenPayload("workflow-instance-1", "bookmark-1");

        var token = _service.CreateToken(payload, DateTimeOffset.UtcNow.AddMinutes(-1));
        var result = _service.TryDecryptToken<TokenPayload>(token, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryDecryptToken_CreatedWithoutExpiration_ReturnsPayload()
    {
        var payload = new TokenPayload("workflow-instance-1", "bookmark-1");

        var token = _service.CreateToken(payload);
        var result = _service.TryDecryptToken<TokenPayload>(token, out var decryptedPayload);

        Assert.StartsWith("v1.ne.", token);
        Assert.True(result);
        AssertPayload(payload, decryptedPayload);
    }

    [Fact]
    public void TryDecryptToken_LegacyTimeLimitedTokenBeforeExpiration_ReturnsPayload()
    {
        var payload = new TokenPayload("workflow-instance-1", "bookmark-1");
        var json = JsonSerializer.Serialize(payload);
        var token = _dataProtectionProvider.CreateProtector("Elsa Tokens").ToTimeLimitedDataProtector().Protect(json, TimeSpan.FromMinutes(5));

        var result = _service.TryDecryptToken<TokenPayload>(token, out var decryptedPayload);

        Assert.True(result);
        AssertPayload(payload, decryptedPayload);
    }

    [Fact]
    public void TryDecryptToken_LegacyNonExpiringToken_ReturnsPayload()
    {
        var payload = new TokenPayload("workflow-instance-1", "bookmark-1");
        var json = JsonSerializer.Serialize(payload);
        var token = _dataProtectionProvider.CreateProtector("Elsa Tokens").Protect(json);

        var result = _service.TryDecryptToken<TokenPayload>(token, out var decryptedPayload);

        Assert.True(result);
        AssertPayload(payload, decryptedPayload);
    }

    [Fact]
    public void TryDecryptToken_StringPayloadCreatedWithPastExpiration_ReturnsFalse()
    {
        var token = _service.CreateToken("payload", DateTimeOffset.UtcNow.AddMinutes(-1));
        var result = _service.TryDecryptToken<string>(token, out _);

        Assert.False(result);
    }

    public void Dispose()
    {
        Directory.Delete(_keyDirectory, true);
    }

    private static void AssertPayload(TokenPayload expected, TokenPayload actual)
    {
        Assert.Equal(expected.WorkflowInstanceId, actual.WorkflowInstanceId);
        Assert.Equal(expected.BookmarkId, actual.BookmarkId);
    }

    private record TokenPayload(string WorkflowInstanceId, string BookmarkId);
}
