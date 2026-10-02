using System.Text.Json;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;
using Microsoft.JSInterop;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Services;

/// <summary>Uses session storage so a PKCE verifier cannot survive the originating browser tab.</summary>
public sealed class BrowserExternalAuthenticationPkceTransactionStore(IJSRuntime jsRuntime) : IExternalAuthenticationPkceTransactionStore
{
    private const string StoragePrefix = "elsa.external-authentication.pkce.";

    /// <inheritdoc />
    public Task SaveAsync(ExternalAuthenticationPkceTransaction transaction, CancellationToken cancellationToken = default) =>
        jsRuntime.InvokeVoidAsync(
            "sessionStorage.setItem",
            cancellationToken,
            GetStorageKey(transaction.State),
            JsonSerializer.Serialize(transaction)).AsTask();

    /// <inheritdoc />
    public async Task<ExternalAuthenticationPkceTransaction?> TakeAsync(string state, CancellationToken cancellationToken = default)
    {
        var key = GetStorageKey(state);
        var json = await jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", cancellationToken, key);
        await jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", cancellationToken, key);

        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<ExternalAuthenticationPkceTransaction>(json);
    }

    private static string GetStorageKey(string state) => StoragePrefix + state;
}
