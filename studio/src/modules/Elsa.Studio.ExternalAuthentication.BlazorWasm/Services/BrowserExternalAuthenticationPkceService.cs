using Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;
using Microsoft.JSInterop;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Services;

/// <summary>Creates S256 PKCE values using the browser's Web Crypto API.</summary>
public sealed class BrowserExternalAuthenticationPkceService(
    IJSRuntime jsRuntime,
    IExternalAuthenticationPkceTransactionStore transactionStore) : IExternalAuthenticationPkceService
{
    /// <inheritdoc />
    public async Task<(ExternalAuthenticationPkceTransaction Transaction, string CodeChallenge)> CreateAsync(
        string? returnPath,
        CancellationToken cancellationToken = default)
    {
        var values = await jsRuntime.InvokeAsync<BrowserPkceValues>(
            "elsaExternalAuthentication.createPkce",
            cancellationToken);
        var transaction = new ExternalAuthenticationPkceTransaction(
            values.State,
            values.CodeVerifier,
            ExternalAuthenticationReturnPath.Normalize(returnPath),
            DateTimeOffset.UtcNow.AddMinutes(10));

        await transactionStore.SaveAsync(transaction, cancellationToken);
        return (transaction, values.CodeChallenge);
    }

    private sealed record BrowserPkceValues(string State, string CodeVerifier, string CodeChallenge);
}
