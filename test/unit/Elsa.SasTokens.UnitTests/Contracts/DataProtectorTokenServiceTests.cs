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

    [Test]
    public async Task DecryptToken_CreatedWithLifetimeBeforeExpiration_ReturnsPayload()
    {
        var payload = new TokenPayload("workflow-instance-1", "bookmark-1");

        var token = _service.CreateToken(payload, TimeSpan.FromMinutes(5));
        var result = _service.DecryptToken<TokenPayload>(token);

        await Assert.That(token).StartsWith("v1.tl.");
        await AssertPayload(payload, result);
    }

    [Test]
    public async Task TryDecryptToken_CreatedWithPastExpiration_ReturnsFalse()
    {
        var payload = new TokenPayload("workflow-instance-1", "bookmark-1");

        var token = _service.CreateToken(payload, DateTimeOffset.UtcNow.AddMinutes(-1));
        var result = _service.TryDecryptToken<TokenPayload>(token, out _);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryDecryptToken_CreatedWithoutExpiration_ReturnsPayload()
    {
        var payload = new TokenPayload("workflow-instance-1", "bookmark-1");

        var token = _service.CreateToken(payload);
        var result = _service.TryDecryptToken<TokenPayload>(token, out var decryptedPayload);

        await Assert.That(token).StartsWith("v1.ne.");
        await Assert.That(result).IsTrue();
        await AssertPayload(payload, decryptedPayload);
    }

    [Test]
    public async Task TryDecryptToken_LegacyTimeLimitedTokenBeforeExpiration_ReturnsPayload()
    {
        var payload = new TokenPayload("workflow-instance-1", "bookmark-1");
        var json = JsonSerializer.Serialize(payload);
        var token = _dataProtectionProvider.CreateProtector("Elsa Tokens").ToTimeLimitedDataProtector().Protect(json, TimeSpan.FromMinutes(5));

        var result = _service.TryDecryptToken<TokenPayload>(token, out var decryptedPayload);

        await Assert.That(result).IsTrue();
        await AssertPayload(payload, decryptedPayload);
    }

    [Test]
    public async Task TryDecryptToken_LegacyNonExpiringToken_ReturnsPayload()
    {
        var payload = new TokenPayload("workflow-instance-1", "bookmark-1");
        var json = JsonSerializer.Serialize(payload);
        var token = _dataProtectionProvider.CreateProtector("Elsa Tokens").Protect(json);

        var result = _service.TryDecryptToken<TokenPayload>(token, out var decryptedPayload);

        await Assert.That(result).IsTrue();
        await AssertPayload(payload, decryptedPayload);
    }

    [Test]
    public async Task TryDecryptToken_StringPayloadCreatedWithPastExpiration_ReturnsFalse()
    {
        var token = _service.CreateToken("payload", DateTimeOffset.UtcNow.AddMinutes(-1));
        var result = _service.TryDecryptToken<string>(token, out _);

        await Assert.That(result).IsFalse();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_keyDirectory))
                Directory.Delete(_keyDirectory, true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Cleanup failures should not fail token behavior tests.
        }
    }

    private static async Task AssertPayload(TokenPayload expected, TokenPayload actual)
    {
        await Assert.That(actual.WorkflowInstanceId).IsEqualTo(expected.WorkflowInstanceId);
        await Assert.That(actual.BookmarkId).IsEqualTo(expected.BookmarkId);
    }

    private record TokenPayload(string WorkflowInstanceId, string BookmarkId);
}
