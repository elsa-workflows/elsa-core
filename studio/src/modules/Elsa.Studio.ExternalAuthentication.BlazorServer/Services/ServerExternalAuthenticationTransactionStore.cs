using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace Elsa.Studio.ExternalAuthentication.BlazorServer.Services;

public sealed record ServerExternalAuthenticationTransaction(
    string State,
    string CodeVerifier,
    string ReturnPath,
    DateTimeOffset ExpiresAt,
    string Purpose = "sign-in");

/// <summary>Stores a short-lived broker transaction in a protected HTTP-only cookie before the callback.</summary>
public interface IServerExternalAuthenticationTransactionStore
{
    void Store(HttpResponse response, ServerExternalAuthenticationTransaction transaction);
    bool TryTake(HttpRequest request, HttpResponse response, out ServerExternalAuthenticationTransaction transaction);
}

internal sealed class ServerExternalAuthenticationTransactionStore(IDataProtectionProvider dataProtectionProvider) : IServerExternalAuthenticationTransactionStore
{
    internal const string CookieName = "ElsaStudio.ExternalAuthentication.Transaction";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("Elsa.Studio.ExternalAuthentication.Transaction.v1");

    public void Store(HttpResponse response, ServerExternalAuthenticationTransaction transaction)
    {
        var protectedValue = _protector.Protect(JsonSerializer.SerializeToUtf8Bytes(transaction));
        response.Cookies.Append(CookieName, WebEncoders.Base64UrlEncode(protectedValue), CookieOptions());
    }

    public bool TryTake(HttpRequest request, HttpResponse response, out ServerExternalAuthenticationTransaction transaction)
    {
        transaction = default!;
        response.Cookies.Delete(CookieName, CookieOptions());
        if (!request.Cookies.TryGetValue(CookieName, out var value))
            return false;

        try
        {
            transaction = JsonSerializer.Deserialize<ServerExternalAuthenticationTransaction>(_protector.Unprotect(WebEncoders.Base64UrlDecode(value)))!;
            return transaction.ExpiresAt > DateTimeOffset.UtcNow;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static CookieOptions CookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        IsEssential = true,
        MaxAge = TimeSpan.FromMinutes(10),
        Path = "/"
    };
}
