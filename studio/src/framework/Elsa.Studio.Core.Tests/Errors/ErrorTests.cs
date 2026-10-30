using System.Net;
using Bunit;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Testing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Core.Tests.Errors;

/// <summary>The shell's error boundary renders <see cref="Error"/> for any failure a page does not handle itself.</summary>
public sealed class ErrorTests : BunitContext, IAsyncLifetime
{
    public ErrorTests()
    {
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer>(new DefaultLocalizer(new StubTranslations([])));
        Services.AddSingleton<IUnauthorizedComponentProvider>(new MarkerUnauthorizedProvider());
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void AForbiddenResponse_ShowsThePermissionGuidanceInsteadOfTheRawException()
    {
        var alert = RenderAlert(ApiExceptions.Create(HttpStatusCode.Forbidden));

        Assert.Equal(AuthorizationFailureExtensions.ForbiddenMessage, alert.Find(".mud-alert-message").TextContent.Trim());
        Assert.Contains("mud-alert-outlined-warning", alert.Find(".mud-alert").ClassName);
        // The lock glyph's outline path (Icons.Material.Outlined.Lock); the markup normalizes the rest of the SVG string.
        Assert.Contains("M18 8h-1V6c0-2.76-2.24-5-5-5S7 3.24 7 6v2H6", alert.Find(".mud-alert-icon").InnerHtml);
    }

    [Fact]
    public void AnUnauthorizedResponse_OffersToSignInAndHandsOverOnlyWhenAsked()
    {
        var cut = RenderAlert(ApiExceptions.Create(HttpStatusCode.Unauthorized));
        Assert.Empty(cut.FindAll(MarkerUnauthorizedProvider.Selector));

        cut.Find("button").Click();

        cut.Find(MarkerUnauthorizedProvider.Selector);
    }

    [Fact]
    public void AForbiddenResponse_DoesNotOfferToSignIn() =>
        Assert.Empty(RenderAlert(ApiExceptions.Create(HttpStatusCode.Forbidden)).FindAll("button"));

    [Fact]
    public void AnyOtherFailure_StillShowsItsTypeAndMessage()
    {
        var alert = RenderAlert(ApiExceptions.Create(HttpStatusCode.InternalServerError));

        Assert.Equal("ApiException: Response status code does not indicate success: 500 (Internal Server Error).", alert.Find(".mud-alert-message").TextContent.Trim());
        Assert.Contains("mud-alert-filled-error", alert.Find(".mud-alert").ClassName);
    }

    private IRenderedComponent<Error> RenderAlert(Exception exception) =>
        Render<Error>(parameters => parameters.Add(x => x.Context, exception));
}
