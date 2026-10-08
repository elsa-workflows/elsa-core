using System.Globalization;
using Bunit;
using Elsa.Studio.Localization.Components;
using Elsa.Studio.Localization.Options;
using Elsa.Studio.Localization.Services;
using Elsa.Studio.Testing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Localization.Tests;

public sealed class LanguagePickerTests : BunitContext, IAsyncLifetime
{
    private readonly CultureInfo _originalUICulture = CultureInfo.CurrentUICulture;

    public LanguagePickerTests()
    {
        // The picker labels its button with the current UI culture, and MudMenu renders no button for the empty
        // name of the invariant culture a CI runner defaults to.
        CultureInfo.CurrentUICulture = new("en-US");
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ICultureService, StubCultureService>();
        // Few enough cultures that MudBlazor's flip logic would not move a menu that opens over its button.
        Services.Configure<LocalizationOptions>(options => options.SupportedCultures = ["en-US", "nl-NL"]);
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        CultureInfo.CurrentUICulture = _originalUICulture;
        await base.DisposeAsync();
    }

    [Fact]
    public void LanguagePicker_OpensBelowItsButton() => MenuPopoverAssert.OpensBelowItsButton<LanguagePicker>(this);

    private sealed class StubCultureService : ICultureService
    {
        public Task ChangeCultureAsync(CultureInfo culture) => Task.CompletedTask;
    }
}
