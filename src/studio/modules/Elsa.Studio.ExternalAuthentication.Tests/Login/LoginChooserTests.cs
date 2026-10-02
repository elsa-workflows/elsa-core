using Bunit;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Authentication.UI.Components;
using Elsa.Studio.Authentication.UI.Extensions;
using Elsa.Studio.Authentication.UI.Services;
using Elsa.Studio.Branding;
using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Elsa.Studio.Localization;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using LoginPage = Elsa.Studio.Authentication.UI.Pages.Login;

namespace Elsa.Studio.ExternalAuthentication.Tests.Login;

public sealed class LoginChooserTests : BunitContext, IAsyncLifetime
{
    public LoginChooserTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddAuthenticationUI();
        Services.AddSingleton<IBrandingProvider, DefaultBrandingProvider>();
        Services.AddSingleton<IClientInformationProvider, StaticClientInformationProvider>();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true).SetVoidResult();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void Ordering_IsStableAcrossLocalAndExternalMethods()
    {
        var methods = LoginMethodChooserState.Order(
        [
            Method("z", "Zulu", "external", 10),
            Method("local", "Elsa account", "local", 0),
            Method("a", "Alpha", "external", 10)
        ]);

        Assert.Equal(["local", "a", "z"], methods.Select(method => method.Key));
    }

    [Fact]
    public void PreferredMethod_IsVisualOnlyAndNeverStartsAutomatically()
    {
        var coordinator = Register(new(
            [Method("contoso", "Contoso", "external", 0, isDefault: true)],
            "contoso"));

        var cut = RenderLoginPage();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Preferred", cut.Markup);
            Assert.Contains("Sign in with Contoso", cut.Markup);
        });
        Assert.Equal(0, coordinator.ExternalBegins);
    }

    [Fact]
    public void ExternalMethods_RenderAsAThemeableProviderList()
    {
        Register(new(
        [
            Method("general", "General", "external", 0, isDefault: true, iconId: "building"),
            Method("workforce", "Workforce", "external", 1, iconId: "microsoft")
        ], "general"));

        var cut = RenderLoginPage();

        cut.WaitForAssertion(() =>
        {
            var list = cut.Find(".elsa-login-panel__external-methods");
            var rows = list.QuerySelectorAll(".elsa-login-panel__method--external");

            Assert.Equal(2, rows.Length);
            Assert.All(rows, row => Assert.NotNull(row.QuerySelector("button.elsa-login-method-button")));
            Assert.Contains("Preferred", rows[0].TextContent);
            Assert.Equal(
                "Sign in with General",
                rows[0].QuerySelector("button")?.GetAttribute("aria-label"));
            Assert.Equal(
                "Sign in with Workforce",
                rows[1].QuerySelector("button")?.GetAttribute("aria-label"));
        });
    }

    [Fact]
    public void TrustedIconRegistry_UsesOnlyRegistrationsAndFallsBackSafely()
    {
        var registry = new LoginMethodIconRegistry([new BuiltInLoginMethodIconProvider()]);

        Assert.Equal("GitHub", registry.Resolve("github").AccessibleName);
        Assert.Equal("Identity provider", registry.Resolve("https://untrusted.example/icon.svg").AccessibleName);
    }

    [Fact]
    public void InvalidReturnPaths_AreNeverForwarded()
    {
        Assert.Equal("/", LocalReturnPath.Normalize("https://attacker.example"));
        Assert.Equal("/", LocalReturnPath.Normalize("//attacker.example"));
        Assert.Equal("/", LocalReturnPath.Normalize("/\\attacker.example"));
        Assert.Equal("/workflows?tab=active", LocalReturnPath.Normalize("/workflows?tab=active"));
    }

    [Fact]
    public void Chooser_RendersTextFirstLocalAndExternalMethods()
    {
        Register(new(
            [Method("local", "Elsa account", "local", 0), Method("github", "GitHub", "external", 1)],
            null));

        var cut = RenderLoginPage();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Elsa account", cut.Markup);
            Assert.Contains("Sign in with GitHub", cut.Markup);
            Assert.Contains("aria-label=\"Sign in with GitHub\"", cut.Markup);
        });
    }

    [Fact]
    public void ServerLocalMethod_UsesStyledFieldsWithoutChangingThePostContract()
    {
        Services.AddSingleton<IExternalAuthenticationAntiforgeryTokenProvider>(
            new FakeAntiforgeryTokenProvider(new("__RequestVerificationToken", "antiforgery-token")));
        Register(
            new([Method("local", "Elsa account", "local", 0)], null),
            localLoginAction: "/authentication/external/local-login");

        var cut = RenderLoginPage();

        cut.WaitForAssertion(() =>
        {
            var form = cut.Find("form[action='/authentication/external/local-login']");
            Assert.Equal("post", form.GetAttribute("method"));
            Assert.NotNull(form.QuerySelector("input[name='__RequestVerificationToken'][value='antiforgery-token']"));
            Assert.NotNull(form.QuerySelector("input[name='returnPath'][value='/']"));
            Assert.Equal(2, form.QuerySelectorAll(".mud-input-control").Length);
            Assert.NotNull(form.QuerySelector("input[name='username'][autocomplete='username']"));
            Assert.NotNull(form.QuerySelector("input[name='password'][type='password'][autocomplete='current-password']"));
            Assert.All(cut.FindComponents<MudTextField<string>>(), field =>
            {
                Assert.Equal(Variant.Outlined, field.Instance.Variant);
                Assert.Equal(Margin.Dense, field.Instance.Margin);
            });
        });
    }

    [Fact]
    public void MethodFailure_ReturnsToChooserWithSafeError()
    {
        Register(
            new([Method("contoso", "Contoso", "external", 0)], null),
            throwExternal: true);

        var cut = RenderLoginPage();
        cut.WaitForAssertion(() => Assert.Contains("Sign in with Contoso", cut.Markup));
        cut.Find("button[aria-label='Sign in with Contoso']").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("selected sign-in method is unavailable", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LocalSignInFailureFromTheServer_IsPresentedWhileKeepingMethodsAvailableForRetry()
    {
        Register(new(
        [
            Method("local", "Elsa account", "local", 0),
            Method("contoso", "Contoso", "external", 1)
        ], null));
        Services.GetRequiredService<NavigationManager>().NavigateTo("/login?choose=true&error=sign_in_failed");

        var cut = RenderLoginPage();

        cut.WaitForAssertion(() =>
        {
            var error = cut.Find("[role='alert']");
            Assert.Contains("Sign-in failed", error.TextContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Elsa account", cut.Markup);
            Assert.NotNull(cut.Find("button[aria-label='Sign in with Contoso']"));
        });
    }

    [Fact]
    public void ExternalSignInFailureFromTheServer_IsPresentedWhileKeepingOtherMethodsAvailable()
    {
        Register(new(
        [
            Method("contoso", "Contoso", "external", 0),
            Method("github", "GitHub", "external", 1)
        ], null));
        Services.GetRequiredService<NavigationManager>().NavigateTo("/login?choose=true&error=external_sign_in_failed");

        var cut = RenderLoginPage();

        cut.WaitForAssertion(() =>
        {
            var error = cut.Find("[role='alert']");
            Assert.Contains("External sign-in failed", error.TextContent, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(cut.Find("button[aria-label='Sign in with GitHub']"));
        });
    }

    [Fact]
    public void NoMethods_ShowsSafeUnavailableState()
    {
        Register(new([], null));

        var cut = RenderLoginPage();

        cut.WaitForAssertion(() => Assert.Contains("No sign-in methods are currently available", cut.Markup));
    }

    [Fact]
    public void SecurityWarning_IsVisibleWhenConfigured()
    {
        Register(
            new([Method("contoso", "Contoso", "external", 0)], null),
            securityWarning: "Credentials are stored in this browser.");

        var cut = RenderLoginPage();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Credentials are stored in this browser.", cut.Markup);
            Assert.Contains("role=\"status\"", cut.Markup);
        });
    }

    [Fact]
    public void Chooser_UsesNamedLandmarkAndTrustedFallback()
    {
        Register(new(
        [
            Method("contoso", "Contoso", "external", 2, iconId: "https://untrusted.example/icon.svg")
        ], null));

        var cut = RenderLoginPage();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("elsa-login-heading", cut.Find("main").GetAttribute("aria-labelledby"));
            Assert.Contains("Sign in with Contoso", cut.Markup);
            Assert.DoesNotContain("https://untrusted.example", cut.Markup, StringComparison.Ordinal);
        });
    }

    private FakeCoordinator Register(
        LoginMethodsResponse response,
        bool throwExternal = false,
        string? securityWarning = null,
        string? localLoginAction = null)
    {
        var coordinator = new FakeCoordinator(response, throwExternal, securityWarning, localLoginAction);
        Services.AddSingleton<IExternalAuthenticationLoginCoordinator>(coordinator);
        Services.AddScoped<ILoginMethodCatalog, ExternalAuthenticationLoginMethodCatalog>();
        Services.AddSingleton<ILoginMethodComponentRegistry>(
            new LoginMethodComponentRegistry(
            [
                new ExternalLoginMethodComponentProvider(),
                new BrokerLocalLoginMethodComponentProvider()
            ]));
        Services.AddSingleton<ILoginMethodIconRegistry>(
            new LoginMethodIconRegistry([new BuiltInLoginMethodIconProvider()]));
        return coordinator;
    }

    private IRenderedComponent<LoginPage> RenderLoginPage()
    {
        Render<MudPopoverProvider>();
        var page = Render<LoginPage>();
        page.FindComponent<LoginThemeHost>().WaitForElement("main");
        return page;
    }

    private static LoginMethodDescriptor Method(
        string key,
        string name,
        string kind,
        int order,
        bool isDefault = false,
        string iconId = "github") =>
        new(key, key, kind, name, iconId, order, isDefault, $"/external-authentication/authorize/{key}");

    private sealed class FakeCoordinator(
        LoginMethodsResponse response,
        bool throwExternal = false,
        string? securityWarning = null,
        string? localLoginAction = null) : IExternalAuthenticationLoginCoordinator
    {
        public int ExternalBegins { get; private set; }
        public string? SecurityWarning => securityWarning;
        public string? LocalLoginAction => localLoginAction;

        public ValueTask<LoginMethodsResponse> DiscoverAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(response);

        public Task BeginExternalAsync(
            LoginMethodDescriptor method,
            string returnPath,
            CancellationToken cancellationToken = default)
        {
            ExternalBegins++;
            return throwExternal ? Task.FromException(new InvalidOperationException()) : Task.CompletedTask;
        }

        public Task BeginLocalAsync(
            string username,
            string password,
            string returnPath,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeAntiforgeryTokenProvider(ExternalAuthenticationAntiforgeryToken token) : IExternalAuthenticationAntiforgeryTokenProvider
    {
        public ExternalAuthenticationAntiforgeryToken GetToken() => token;
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
