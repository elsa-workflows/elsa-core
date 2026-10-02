using System.Text.Json;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;
using Microsoft.JSInterop;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Services;

/// <summary>Stores broker credentials in memory by default and only persists them when explicitly configured.</summary>
public sealed class BrowserExternalAuthenticationTokenStore(
    IJSRuntime jsRuntime,
    ExternalAuthenticationWasmOptions options) : IExternalAuthenticationBrowserTokenStore
{
    private const string StorageKey = "elsa.external-authentication.tokens";
    private ExternalAuthenticationTokenSet? memoryTokens;

    /// <inheritdoc />
    public async Task<ExternalAuthenticationTokenSet?> GetAsync(CancellationToken cancellationToken = default)
    {
        if (options.BrowserStorage == ExternalAuthenticationBrowserStorageMode.Memory)
            return memoryTokens;

        var json = await jsRuntime.InvokeAsync<string?>(GetItemOperation, cancellationToken, StorageKey);
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<ExternalAuthenticationTokenSet>(json);
    }

    /// <inheritdoc />
    public async Task SetAsync(ExternalAuthenticationTokenSet tokens, CancellationToken cancellationToken = default)
    {
        memoryTokens = tokens;

        if (options.BrowserStorage != ExternalAuthenticationBrowserStorageMode.Memory)
            await jsRuntime.InvokeVoidAsync(SetItemOperation, cancellationToken, StorageKey, JsonSerializer.Serialize(tokens));
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        memoryTokens = null;

        if (options.BrowserStorage != ExternalAuthenticationBrowserStorageMode.Memory)
            await jsRuntime.InvokeVoidAsync(RemoveItemOperation, cancellationToken, StorageKey);
    }

    private string GetItemOperation => options.BrowserStorage == ExternalAuthenticationBrowserStorageMode.Session
        ? "sessionStorage.getItem"
        : "localStorage.getItem";

    private string SetItemOperation => options.BrowserStorage == ExternalAuthenticationBrowserStorageMode.Session
        ? "sessionStorage.setItem"
        : "localStorage.setItem";

    private string RemoveItemOperation => options.BrowserStorage == ExternalAuthenticationBrowserStorageMode.Session
        ? "sessionStorage.removeItem"
        : "localStorage.removeItem";
}
