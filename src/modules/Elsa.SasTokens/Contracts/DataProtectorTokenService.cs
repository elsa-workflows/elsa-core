using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Elsa.SasTokens.Contracts;

/// <summary>
/// A service that can create and decrypt SAS (Shared Access Signature) tokens using the <see cref="Microsoft.AspNetCore.DataProtection.IDataProtector"/> service.
/// </summary>
public class DataProtectorTokenService : ITokenService
{
    private readonly IDataProtector _dataProtector;
    private readonly ITimeLimitedDataProtector _timeLimitedDataProtector;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataProtectorTokenService"/> class.
    /// </summary>
    public DataProtectorTokenService(IDataProtectionProvider dataProtector)
    {
        _dataProtector = dataProtector.CreateProtector("Elsa Tokens");
        _timeLimitedDataProtector = _dataProtector.ToTimeLimitedDataProtector();
    }

    /// <inheritdoc />
    public string CreateToken<T>(T payload, TimeSpan lifetime)
    {
        var json = JsonSerializer.Serialize(payload);
        return _timeLimitedDataProtector.Protect(json, lifetime);
    }
    
    /// <inheritdoc />
    public string CreateToken<T>(T payload, DateTimeOffset expiresAt)
    {
        var json = JsonSerializer.Serialize(payload);
        return _timeLimitedDataProtector.Protect(json, expiresAt);
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
        var json = Unprotect(token);
        return JsonSerializer.Deserialize<T>(json)!;
    }

    private string Unprotect(string token)
    {
        try
        {
            return _timeLimitedDataProtector.Unprotect(token);
        }
        catch (CryptographicException)
        {
            var json = _dataProtector.Unprotect(token);

            // Expired time-limited tokens can be decrypted by the base protector,
            // but the result includes the expiration header.
            // Only accept fallback payloads that are standalone JSON produced by CreateToken(payload).
            try
            {
                using var _ = JsonDocument.Parse(json);
                return json;
            }
            catch (JsonException e)
            {
                throw new CryptographicException("Token payload is not a valid non-expiring token.", e);
            }
        }
    }
}
