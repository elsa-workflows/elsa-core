using Blazored.LocalStorage;
using Elsa.Studio.Login.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;

namespace Elsa.Studio.Login.BlazorServer.Services;

/// <summary>
/// Implements the <see cref="IJwtAccessor"/> interface for server-side Blazor.
/// </summary>
public class BlazorServerJwtAccessor : IJwtAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILocalStorageService _localStorageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorServerJwtAccessor"/> class.
    /// </summary>
    public BlazorServerJwtAccessor(IHttpContextAccessor httpContextAccessor, ILocalStorageService localStorageService)
    {
        _httpContextAccessor = httpContextAccessor;
        _localStorageService = localStorageService;
    }

    /// <inheritdoc />
    public async ValueTask<string?> ReadTokenAsync(string name)
    {
        if (IsPrerendering())
            return null;

        try
        {
            return await _localStorageService.GetItemAsync<string>(name);
        }
        catch (InvalidOperationException e) when (IsJavaScriptInteropUnavailable(e))
        {
            return null;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async ValueTask WriteTokenAsync(string name, string token)
    {
        if (IsPrerendering())
            return;

        try
        {
            await _localStorageService.SetItemAsStringAsync(name, token);
        }
        catch (InvalidOperationException e) when (IsJavaScriptInteropUnavailable(e))
        {
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private bool IsPrerendering() => _httpContextAccessor.HttpContext?.Response.HasStarted == false;

    private static bool IsJavaScriptInteropUnavailable(InvalidOperationException e) =>
        e.Message.Contains("JavaScript interop calls cannot be issued at this time", StringComparison.Ordinal);
}
