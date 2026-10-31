using Blazored.LocalStorage;
using Elsa.Studio.Login.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;

namespace Elsa.Studio.BlazorServer.Services; // your host's namespace

/// <summary>
/// Backport of the 3.7.x prerender/JS-interop guard for BlazorServerJwtAccessor,
/// so token reads during the pre-interactive circuit render don't crash on 3.6.3.
/// </summary>
public class SafeBlazorServerJwtAccessor : IJwtAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILocalStorageService _localStorageService;

    public SafeBlazorServerJwtAccessor(
        IHttpContextAccessor httpContextAccessor,
        ILocalStorageService localStorageService)
    {
        _httpContextAccessor = httpContextAccessor;
        _localStorageService = localStorageService;
    }

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

    private bool IsPrerendering() =>
        _httpContextAccessor.HttpContext?.Response.HasStarted == false;

    private static bool IsJavaScriptInteropUnavailable(InvalidOperationException e) =>
        e.Message.Contains("JavaScript interop calls cannot be issued at this time", StringComparison.Ordinal);
}