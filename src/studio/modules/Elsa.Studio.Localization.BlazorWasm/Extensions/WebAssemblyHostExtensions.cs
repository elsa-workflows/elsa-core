using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Globalization;

namespace Elsa.Studio.Localization.BlazorWasm.Extensions;

/// <summary>
/// Provides WebAssembly host extensions that configure localization.
/// </summary>
public static class WebAssemblyHostExtensions
{
    /// <summary>
    /// Loads the persisted culture from the browser and applies it to the Blazor host.
    /// </summary>
    /// <param name="host">The WebAssembly host to configure.</param>
    /// <returns>The configured host instance.</returns>
    public static async Task<WebAssemblyHost> UseElsaLocalization(this WebAssemblyHost host) {

        const string defaultCulture = "en-US";
        var js = host.Services.GetRequiredService<IJSRuntime>();
        await js.InvokeAsync<IJSObjectReference>("import", "./_content/Elsa.Studio.Localization.BlazorWasm/blazorCulture.js");
        var result = await js.InvokeAsync<string?>("blazorCulture.get");
        var culture = CultureInfo.GetCultureInfo(result ?? defaultCulture);

        if (result == null)
        {
            await js.InvokeVoidAsync("blazorCulture.set", defaultCulture);
        }

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        return host;
    }
}