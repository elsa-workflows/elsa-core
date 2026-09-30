using System.Net;
using Bunit;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Secrets.Components;
using Elsa.Studio.Testing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using SecretPage = Elsa.Studio.Secrets.Pages.Secret;

namespace Elsa.Studio.Administration.Tests;

/// <summary>
/// When the backend refuses a call Studio could not hide ahead of time, the user reads what to do about it rather than
/// the API client's raw "Response status code does not indicate success: 403 (Forbidden).".
/// </summary>
public sealed class ForbiddenResponseTests : BunitContext, IAsyncLifetime
{
    public ForbiddenResponseTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer>(new TestLocalizer());
        Services.AddSingleton<IBackendApiClientProvider>(new ForbiddingBackend());
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    private ISnackbar Snackbar => Services.GetRequiredService<ISnackbar>();

    [Fact]
    public void SecretPicker_ShowsThePermissionGuidance_WhenTheBackendRefusesToListSecrets()
    {
        var cut = Render<SecretPicker>();

        cut.WaitForAssertion(() => Assert.Equal(AuthorizationFailureExtensions.ForbiddenMessage, Assert.Single(Snackbar.ShownSnackbars).Message));
    }

    [Fact]
    public void SecretPage_ShowsThePermissionGuidance_WhenTheBackendRefusesToLoadTheSecret()
    {
        var cut = Render<SecretPage>(parameters => parameters
            .AddCascadingValue(StubPermissionService.Grants("secrets:view"))
            .Add(x => x.Name, "api-key"));

        cut.WaitForAssertion(() => Assert.Contains(AuthorizationFailureExtensions.ForbiddenMessage, cut.Markup));
        Assert.Equal(AuthorizationFailureExtensions.ForbiddenMessage, Assert.Single(Snackbar.ShownSnackbars).Message);
        Assert.DoesNotContain("403 (Forbidden)", cut.Markup);
    }
}
