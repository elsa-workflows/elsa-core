using System.Security.Claims;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Bunit;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Components;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Controllers;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Compatibility;

/// <summary>
/// On Blazor Server the OpenID Connect user menu posts an antiforgery-protected sign-out that ends the cookie session
/// and, when the provider advertises an end_session_endpoint, performs RP-initiated logout.
/// </summary>
public sealed class OpenIdConnectBlazorServerSignOutTests
    : OpenIdConnectUserMenuTests<OpenIdConnectBlazorServerFeature, OpenIdConnectUserMenu>
{
    private const string IdToken = "id-token";
    private const string EndSessionEndpoint = "https://idp.example/logout";
    private const string SignOutPath = "/authentication/logout";
    private static readonly RendererInfo Prerender = new("Static", isInteractive: false);
    private static readonly RendererInfo Circuit = new("Server", isInteractive: true);
    private readonly OpenIdConnectConfiguration _provider = new() { EndSessionEndpoint = EndSessionEndpoint };
    private readonly StubHttpContextAccessor _httpContextAccessor = new();
    private readonly PersistedAntiforgeryState _persistedAntiforgery = new();
    private readonly Dictionary<string, string> _browserCookies = [];
    private readonly WebApplication _studio;
    private readonly RequestDelegate _handleRequest;
    private ClaimsPrincipal _user = new(new ClaimsIdentity());

    public OpenIdConnectBlazorServerSignOutTests()
    {
        // The Server host's services and request pipeline, down to the controller and the authentication handlers.
        var host = WebApplication.CreateSlimBuilder();
        host.Services.AddRazorPages().AddApplicationPart(typeof(AuthenticationController).Assembly);
        host.Services.AddOpenIdConnectAuth(ConfigureIdentityProvider);
        host.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options => options.Configuration = _provider);
        host.Services.AddSingleton<ISingleFlightCoordinator, SingleFlightCoordinator>();
        _studio = host.Build();
        var app = new ApplicationBuilder(_studio.Services);
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
        _handleRequest = app.Build();

        // Blazor Server renders in the same app, so the menu issues antiforgery tokens with the host's keys.
        Services.AddOpenIdConnectAuth(ConfigureIdentityProvider);
        Services.AddSingleton(_studio.Services.GetRequiredService<IAntiforgery>());
        Services.AddSingleton<IHttpContextAccessor>(_httpContextAccessor);
        Services.AddSingleton<AntiforgeryStateProvider>(_persistedAntiforgery);
    }

    private CookieAuthenticationOptions SessionCookieOptions =>
        _studio.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);

    [Fact]
    public async Task SignOutFromTheLiveCircuit_EndsTheLocalSessionAndRedirectsToTheProvidersEndSessionEndpoint()
    {
        SignIn();

        var response = await SignOutFromTheLiveCircuitAsync();

        var endSession = new Uri(response.Headers.Location.ToString());
        var query = QueryHelpers.ParseQuery(endSession.Query);
        Assert.True(EndsTheSession(response));
        Assert.Equal(StatusCodes.Status302Found, response.StatusCode);
        Assert.Equal(EndSessionEndpoint, endSession.GetLeftPart(UriPartial.Path));
        Assert.Equal(IdToken, query["id_token_hint"]);
        Assert.Equal("https://studio.example/signout-callback-oidc", query["post_logout_redirect_uri"]);
    }

    [Fact]
    public async Task SignOutFromALongPollingCircuit_UsesTheAntiforgeryTokenThePrerenderPersisted()
    {
        SignIn();

        var response = await SignOutFromTheLiveCircuitAsync(longPolling: true);

        Assert.True(EndsTheSession(response));
        Assert.Equal(StatusCodes.Status302Found, response.StatusCode);
    }

    [Fact]
    public async Task SignOutWithoutAnEndSessionEndpoint_EndsTheLocalSessionOnly()
    {
        _provider.EndSessionEndpoint = null;
        SignIn();

        var response = await SignOutFromTheLiveCircuitAsync(returnUrl: "/workflows");

        Assert.True(EndsTheSession(response));
        Assert.Equal(StatusCodes.Status302Found, response.StatusCode);
        Assert.Equal("/workflows", response.Headers.Location.ToString());
    }

    [Fact]
    public void AnonymousUser_IsNotIssuedAnAntiforgeryCookie()
    {
        var page = BrowserRequest(HttpMethods.Get, "/");

        RenderWhileHandling(page, Prerender);

        Assert.Empty(SetCookies(page.Response));
    }

    [Fact]
    public async Task SignOutEndpoint_DoesNotSignOutOnGet()
    {
        SignIn();

        var response = await SendAsync(BrowserRequest(HttpMethods.Get, SignOutPath));

        Assert.Equal(StatusCodes.Status405MethodNotAllowed, response.StatusCode);
        Assert.False(EndsTheSession(response));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("forged-token")]
    public async Task SignOutEndpoint_RejectsAPostWithoutAValidAntiforgeryToken(string? token)
    {
        SignIn();
        var fields = token is null ? [] : new Dictionary<string, StringValues> { ["__RequestVerificationToken"] = token };

        var response = await SendAsync(FormRequest(HttpMethods.Post, SignOutPath, fields));

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.False(EndsTheSession(response));
    }

    protected override void SignIn()
    {
        base.SignIn();
        _user = new(new ClaimsIdentity(
            [new Claim("sub", UserName), new Claim("name", UserName)], CookieAuthenticationDefaults.AuthenticationScheme, "name", "role"));
        var properties = new AuthenticationProperties();
        properties.StoreTokens([new AuthenticationToken { Name = "id_token", Value = IdToken }]);
        var ticket = new AuthenticationTicket(_user, properties, CookieAuthenticationDefaults.AuthenticationScheme);
        _browserCookies[SessionCookieOptions.Cookie.Name!] = SessionCookieOptions.TicketDataFormat.Protect(ticket);
    }

    /// <summary>
    /// Renders the menu the way the host's ServerPrerendered mode does, then clicks Sign out. The menu is prerendered
    /// while handling the page request, whose cookies the browser keeps, and rendered again in the live circuit. There
    /// the HttpContext is the circuit connection's request, whose response has already started, so nothing rendered in
    /// the circuit can set a cookie. On long polling the circuit has no HttpContext at all, only the antiforgery token that
    /// the prerender persisted in the page.
    /// </summary>
    private async Task<HttpResponse> SignOutFromTheLiveCircuitAsync(string? returnUrl = null, bool longPolling = false)
    {
        var page = BrowserRequest(HttpMethods.Get, "/");
        RenderWhileHandling(page, Prerender);
        foreach (var cookie in SetCookies(page.Response))
            _browserCookies[cookie.Name.ToString()] = cookie.Value.ToString();
        await DisposeComponentsAsync();

        // The framework persists the token the prerender issued, so that a circuit without an HttpContext can post it back.
        if (longPolling)
            _persistedAntiforgery.Persist(_studio.Services.GetRequiredService<IAntiforgery>().GetAndStoreTokens(page));
        var circuitRequest = longPolling ? null : StartedResponse(BrowserRequest(HttpMethods.Get, "/_blazor"));
        var menu = RenderWhileHandling(circuitRequest, Circuit);
        var signOut = FindInOpenMenu(menu, "button[type=submit]");
        Assert.Equal("Sign out", signOut.TextContent.Trim());

        // Clicking the button submits its form.
        var form = ((IHtmlButtonElement)signOut).Form!;
        var fields = form.QuerySelectorAll("input").ToDictionary(input => input.GetAttribute("name")!, input => new StringValues(input.GetAttribute("value")));
        if (returnUrl is not null)
            fields["returnUrl"] = returnUrl;
        return await SendAsync(FormRequest(form.Method.ToUpperInvariant(), form.GetAttribute("action")!, fields));
    }

    /// <summary>Renders the menu while Blazor Server handles <paramref name="request"/>, authenticated as the host would, or without any request.</summary>
    private IRenderedComponent<OpenIdConnectUserMenu> RenderWhileHandling(HttpContext? request, RendererInfo renderer)
    {
        if (request is not null)
            request.User = _user;
        _httpContextAccessor.HttpContext = request;
        SetRendererInfo(renderer);
        return RenderAppBarMenu();
    }

    private async Task<HttpResponse> SendAsync(HttpContext request)
    {
        await using var scope = _studio.Services.CreateAsyncScope();
        request.RequestServices = scope.ServiceProvider;
        await _handleRequest(request);
        return request.Response;
    }

    /// <summary>A request from the browser to Studio, carrying the cookies the browser holds.</summary>
    private DefaultHttpContext BrowserRequest(string method, string path) => new()
    {
        Request =
        {
            Method = method,
            Scheme = "https",
            Host = new HostString("studio.example"),
            Path = path,
            Headers = { Cookie = string.Join("; ", _browserCookies.Select(cookie => $"{cookie.Key}={cookie.Value}")) }
        }
    };

    /// <summary>Marks the response as started, so that setting any cookie or header on it throws, as it does on the wire.</summary>
    private static DefaultHttpContext StartedResponse(DefaultHttpContext request)
    {
        request.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());
        return request;
    }

    private DefaultHttpContext FormRequest(string method, string path, Dictionary<string, StringValues> fields)
    {
        var request = BrowserRequest(method, path);
        request.Request.ContentType = "application/x-www-form-urlencoded";
        request.Request.Form = new FormCollection(fields);
        return request;
    }

    private bool EndsTheSession(HttpResponse response) =>
        SetCookies(response).Any(cookie => cookie.Name == SessionCookieOptions.Cookie.Name && cookie.Expires < DateTimeOffset.UtcNow);

    private static IList<SetCookieHeaderValue> SetCookies(HttpResponse response) =>
        SetCookieHeaderValue.ParseList(response.Headers.SetCookie.ToArray()!);

    protected override async ValueTask DisposeAsyncCore()
    {
        await _studio.DisposeAsync();
        await base.DisposeAsyncCore();
    }

    private sealed class StartedResponseFeature : HttpResponseFeature
    {
        public StartedResponseFeature() => Headers = new HeaderDictionary { IsReadOnly = true };

        public override bool HasStarted => true;
    }

    /// <summary>The antiforgery token the prerender persisted in the page, which the circuit's provider hands back.</summary>
    private sealed class PersistedAntiforgeryState : AntiforgeryStateProvider
    {
        private AntiforgeryRequestToken? _token;

        public void Persist(AntiforgeryTokenSet tokens) => _token = new AntiforgeryRequestToken(tokens.RequestToken!, tokens.FormFieldName);

        public override AntiforgeryRequestToken? GetAntiforgeryToken() => _token;
    }

    private sealed class StubHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
