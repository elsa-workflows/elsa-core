using Elsa.Studio.Localization.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;

namespace Elsa.Studio.Localization.BlazorWasm.Services;

/// <inheritdoc />
public class BlazorWasmCultureService(NavigationManager navigationManager, IJSRuntime jSRuntime) : ICultureService
{
    private IJSObjectReference? _module;

    /// <inheritdoc />
    public async Task ChangeCultureAsync(CultureInfo culture)
    {
        if (CultureInfo.CurrentUICulture.Name != culture.Name)
        {
            _module = await jSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Elsa.Studio.Localization.BlazorWasm/blazorCulture.js");
            await jSRuntime.InvokeVoidAsync("blazorCulture.set", culture.Name);

            navigationManager.NavigateTo(navigationManager.Uri, forceLoad: true);
        }
    }
}