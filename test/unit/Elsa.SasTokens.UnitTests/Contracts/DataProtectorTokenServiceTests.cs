using Elsa.SasTokens.Contracts;
using Microsoft.AspNetCore.DataProtection;

namespace Elsa.SasTokens.UnitTests.Contracts;

public class DataProtectorTokenServiceTests : IDisposable
{
    private readonly string _keyDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly DataProtectorTokenService _service;

    public DataProtectorTokenServiceTests()
    {
        Directory.CreateDirectory(_keyDirectory);
        _service = new DataProtectorTokenService(DataProtectionProvider.Create(new DirectoryInfo(_keyDirectory)));
    }

    [Fact]
    public void DecryptToken_CreatedWithLifetimeBeforeExpiration_ReturnsPayload()
    {
        var payload = new TokenPayload("workflow-instance-1", "bookmark-1");

        var token = _service.CreateToken(payload, TimeSpan.FromMinutes(5));
        var result = _service.DecryptToken<TokenPayload>(token);

        Assert.Equal(payload.WorkflowInstanceId, result.WorkflowInstanceId);
        Assert.Equal(payload.BookmarkId, result.BookmarkId);
    }

    [Fact]
    public void TryDecryptToken_CreatedWithPastExpiration_ReturnsFalse()
    {
        var payload = new TokenPayload("workflow-instance-1", "bookmark-1");

        var token = _service.CreateToken(payload, DateTimeOffset.UtcNow.AddMinutes(-1));
        var result = _service.TryDecryptToken<TokenPayload>(token, out _);

        Assert.False(result);
    }

    public void Dispose()
    {
        Directory.Delete(_keyDirectory, true);
    }

    private record TokenPayload(string WorkflowInstanceId, string BookmarkId);
}
