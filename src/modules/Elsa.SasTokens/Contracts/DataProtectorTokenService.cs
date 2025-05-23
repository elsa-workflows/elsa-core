using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Elsa.SasTokens.Contracts;

/// <summary>
/// A service that can create and decrypt SAS (Shared Access Signature) tokens using the <see cref="Microsoft.AspNetCore.DataProtection.IDataProtector"/> service.
/// </summary>
public class DataProtectorTokenService : ITokenService
{
    private readonly IDataProtector _dataProtector;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataProtectorTokenService"/> class.
    /// </summary>
    public DataProtectorTokenService(IDataProtectionProvider dataProtector)
    {
        _dataProtector = dataProtector.CreateProtector("Elsa Tokens");
    }

    /// <inheritdoc />
    public string CreateToken<T>(T payload, TimeSpan lifetime)
    {
        var json = JsonSerializer.Serialize(payload);
        return _dataProtector.ToTimeLimitedDataProtector().Protect(json, lifetime);
    }
    
    /// <inheritdoc />
    public string CreateToken<T>(T payload, DateTimeOffset expiresAt)
    {
        var json = JsonSerializer.Serialize(payload);
        return _dataProtector.ToTimeLimitedDataProtector().Protect(json, expiresAt);
    }
    
    /// <inheritdoc />
    public string CreateToken<T>(T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return _dataProtector.Protect(json);
    }

    /// <inheritdoc />
    public bool TryDecryptToken<T>(string token, out T payload)
    {
        payload = default!;

        try
        {
            payload = DecryptToken<T>(token);
            return true;
        }
        catch
        {
            // ignored.
        }

        return false;
    }

    /// <inheritdoc />
    public T DecryptToken<T>(string token)
    {
        var json = _dataProtector.Unprotect(token);
        return JsonSerializer.Deserialize<T>(json)!;
    }
}