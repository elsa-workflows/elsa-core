using System.Net;
using Bunit;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Layouts;
using Elsa.Studio.Localization;
using Elsa.Studio.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;
using DefaultBrandingProvider = Elsa.Studio.Branding.DefaultBrandingProvider;
using IBrandingProvider = Elsa.Studio.Branding.IBrandingProvider;

namespace Elsa.Studio.Core.Tests.Errors;

/// <summary>
/// A page failure the shell cannot recover from is shown by the error boundary. Only an <see cref="UnauthorizedAccessException"/>
/// hands over to the sign-in component; a 401 response is explained like any other authorization failure.
/// </summary>
public sealed class MainLayoutErrorBoundaryTests : BunitContext, IAsyncLifetime
{
    public MainLayoutErrorBoundaryTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddCoreInternal();
        Services.AddSingleton<ILocalizer>(new DefaultLocalizer(new StubTranslations([])));
        Services.AddSingleton<IFeatureService, NoFeatures>();
        Services.AddSingleton<IBrandingProvider, DefaultBrandingProvider>();
        Services.AddSingleton<IUnauthorizedComponentProvider>(new MarkerUnauthorizedProvider());
        Services.AddSingleton<IErrorComponentProvider>(new MarkerErrorProvider());
        ComponentFactories.AddStub<NavMenu>();
        ComponentFactories.AddStub<PermissionPageGuard>(parameters => parameters.Get(x => x.ChildContent)!);
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    // Neither auth handler reacts to a 401 response, so redirecting to sign-in from here would loop for a user whose
    // token the backend keeps rejecting. The boundary shows the guidance and leaves signing out to the user.
    [Theory]
    [MemberData(nameof(RejectedSignIns))]
    public void ARejectedSignIn_ShowsTheGuidanceInsteadOfRedirecting(Exception failure)
    {
        var cut = RenderLayoutWith(failure);

        Assert.Equal(AuthorizationFailureExtensions.UnauthorizedMessage, cut.Find("#error-display .mud-alert-message").TextContent.Trim());
        Assert.Empty(cut.FindAll(MarkerUnauthorizedProvider.Selector));
    }

    [Fact]
    public void AnUnauthorizedAccessException_HandsOverToTheSignInComponent()
    {
        var cut = RenderLayoutWith(new UnauthorizedAccessException());

        cut.Find(MarkerUnauthorizedProvider.Selector);
        Assert.Empty(cut.FindAll("#error-display"));
    }

    [Theory]
    [MemberData(nameof(OtherFailures))]
    public void AnyOtherFailure_IsDisplayedInsteadOfRedirecting(Exception failure, string expectedText)
    {
        var cut = RenderLayoutWith(failure);

        Assert.Contains(expectedText, cut.Find("#error-display").TextContent);
        Assert.Empty(cut.FindAll("#sign-in-redirect"));
    }

    public static TheoryData<Exception> RejectedSignIns => new()
    {
        ApiExceptions.Create(HttpStatusCode.Unauthorized),
        new HttpRequestException("Response status code does not indicate success: 401 (Unauthorized).", null, HttpStatusCode.Unauthorized)
    };

    public static TheoryData<Exception, string> OtherFailures => new()
    {
        { ApiExceptions.Create(HttpStatusCode.Forbidden), AuthorizationFailureExtensions.ForbiddenMessage },
        { ApiExceptions.Create(HttpStatusCode.InternalServerError), "500" },
        { new InvalidOperationException("boom"), "boom" }
    };

    private IRenderedComponent<MainLayout> RenderLayoutWith(Exception failure) =>
        Render<MainLayout>(parameters => parameters.Add(x => x.Body, builder =>
        {
            builder.OpenComponent<FailingPage>(0);
            builder.AddAttribute(1, nameof(FailingPage.Failure), failure);
            builder.CloseComponent();
        }));

    private sealed class FailingPage : ComponentBase
    {
        [Parameter] public Exception Failure { get; set; } = null!;

        protected override void OnInitialized() => throw Failure;
    }

    private sealed class NoFeatures : IFeatureService
    {
        public event Action? Initialized { add { } remove { } }
        public IEnumerable<IFeature> GetFeatures() => [];
        public Task InitializeFeaturesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MarkerErrorProvider : IErrorComponentProvider
    {
        public RenderFragment GetErrorComponent(Exception context) => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "id", "error-display");
            builder.OpenComponent<Error>(2);
            builder.AddAttribute(3, nameof(Error.Context), context);
            builder.CloseComponent();
            builder.CloseElement();
        };
    }
}
