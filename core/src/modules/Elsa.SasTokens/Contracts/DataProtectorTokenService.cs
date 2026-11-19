using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Elsa.SasTokens.Contracts;

/// <summary>
/// A service that can create and decrypt SAS (Shared Access Signature) tokens using the <see cref="Microsoft.AspNetCore.DataProtection.IDataProtector"/> service.
/// </summary>
public class DataProtectorTokenService : ITokenService
{
    private const string TimeLimitedTokenPrefix = "v2.tl.";
    private const string NonExpiringTokenPrefix = "v2.ne.";
    private const string LegacyTimeLimitedPrefix = "v1.tl.";
    private const string LegacyNonExpiringPrefix = "v1.ne.";
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
        return TimeLimitedTokenPrefix + GetProtector<T>().ToTimeLimitedDataProtector().Protect(json, lifetime);
    }
    
    /// <inheritdoc />
    public string CreateToken<T>(T payload, DateTimeOffset expiresAt)
    {
        var json = JsonSerializer.Serialize(payload);
        return TimeLimitedTokenPrefix + GetProtector<T>().ToTimeLimitedDataProtector().Protect(json, expiresAt);
    }
    
    /// <inheritdoc />
    public string CreateToken<T>(T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return NonExpiringTokenPrefix + GetProtector<T>().Protect(json);
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
        var json = Unprotect<T>(token);
        return JsonSerializer.Deserialize<T>(json) ?? throw new JsonException("Token payload is missing.");
    }

    private IDataProtector GetProtector<T>()
    {
        // Omit assembly versions so package upgrades can still read outstanding links.
        var type = typeof(T);
        return _dataProtector.CreateProtector("v2", type.Assembly.GetName().Name!, type.ToString());
    }

    private string Unprotect<T>(string token)
    {
        if (token.StartsWith(TimeLimitedTokenPrefix, StringComparison.Ordinal))
            return GetProtector<T>().ToTimeLimitedDataProtector().Unprotect(token[TimeLimitedTokenPrefix.Length..]);

        if (token.StartsWith(NonExpiringTokenPrefix, StringComparison.Ordinal))
            return GetProtector<T>().Unprotect(token[NonExpiringTokenPrefix.Length..]);

        if (token.StartsWith(LegacyTimeLimitedPrefix, StringComparison.Ordinal))
            return _timeLimitedDataProtector.Unprotect(token[LegacyTimeLimitedPrefix.Length..]);

        if (token.StartsWith(LegacyNonExpiringPrefix, StringComparison.Ordinal))
            return _dataProtector.Unprotect(token[LegacyNonExpiringPrefix.Length..]);

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
