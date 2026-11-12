using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Elsa.Extensions;
using Elsa.Http.Bookmarks;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Http.IntegrationTests.Authorization;

/// <summary>
/// Sends requests through <see cref="Elsa.Http.Middleware.HttpWorkflowsMiddleware"/> to verify that the options of an HttpEndpoint
/// are enforced both when the endpoint starts a workflow and when a suspended workflow is resumed at that endpoint.
/// </summary>
public class HttpEndpointAuthorizationTests : IAsyncLifetime
{
    private const string UserHeader = "X-Test-User";
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>("Test", null);
        builder.Services.AddAuthorization(options => options.AddPolicy(PolicyWorkflow.PolicyName, policy => policy.RequireClaim(ClaimTypes.Role, "admin")));
        builder.Services.AddElsa(elsa => elsa
            .UseWorkflowManagement()
            .UseWorkflowRuntime()
            .UseWorkflows()
            .UseHttp()
            .AddWorkflow<AuthorizeWorkflow>()
            .AddWorkflow<PolicyWorkflow>()
            .AddWorkflow<SecondStageProtectedWorkflow>());

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseWorkflows();
        await _app.StartAsync();
        await _app.Services.PopulateRegistriesAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    // Anonymous, authenticated (but not an admin), and an admin.
    [Theory(DisplayName = "Authorize=true: start is enforced for anonymous and authenticated callers")]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("viewer", HttpStatusCode.OK)]
    [InlineData("admin", HttpStatusCode.OK)]
    public async Task Authorize_Start(string? user, HttpStatusCode expected)
    {
        var response = await PostAsync(new AuthorizeWorkflow().StartPath, user);
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(expected == HttpStatusCode.OK ? 1 : 0, (await FindBookmarksAsync(new AuthorizeWorkflow().ResumePath)).Count);
    }

    [Theory(DisplayName = "Authorize=true: resume is enforced for anonymous and authenticated callers")]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("viewer", HttpStatusCode.OK)]
    [InlineData("admin", HttpStatusCode.OK)]
    public async Task Authorize_Resume(string? user, HttpStatusCode expected)
    {
        await AssertResumeAsync(new AuthorizeWorkflow(), user, expected);
    }

    [Theory(DisplayName = "Policy: start is enforced for anonymous, wrong-policy and matching callers")]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("viewer", HttpStatusCode.Unauthorized)]
    [InlineData("admin", HttpStatusCode.OK)]
    public async Task Policy_Start(string? user, HttpStatusCode expected)
    {
        var workflow = new PolicyWorkflow();
        var response = await PostAsync(workflow.StartPath, user);
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(expected == HttpStatusCode.OK ? 1 : 0, (await FindBookmarksAsync(workflow.ResumePath)).Count);
    }

    [Theory(DisplayName = "Policy: resume is enforced for anonymous, wrong-policy and matching callers")]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("viewer", HttpStatusCode.Unauthorized)]
    [InlineData("admin", HttpStatusCode.OK)]
    public async Task Policy_Resume(string? user, HttpStatusCode expected)
    {
        await AssertResumeAsync(new PolicyWorkflow(), user, expected);
    }

    [Fact(DisplayName = "Only the second stage is protected: the start is open, the resume is enforced")]
    public async Task SecondStageProtected_StartIsOpenAndResumeIsEnforced()
    {
        var workflow = new SecondStageProtectedWorkflow();
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(workflow.StartPath, user: null)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsync(workflow.ResumePath, user: null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsync(workflow.ResumePath, user: "viewer")).StatusCode);
        Assert.Single(await FindBookmarksAsync(workflow.ResumePath));

        var response = await PostAsync(workflow.ResumePath, user: "admin");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("done", await response.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "The bookmark of a suspended HttpEndpoint carries the options configured on the activity")]
    public async Task ResumeBookmark_PayloadMatchesTheActivityOptions()
    {
        var workflow = new SecondStageProtectedWorkflow();
        await PostAsync(workflow.StartPath, user: null);

        var bookmark = Assert.Single(await FindBookmarksAsync(workflow.ResumePath));
        var payload = bookmark.GetPayload<HttpEndpointBookmarkPayload>();

        Assert.Equal("/" + workflow.ResumePath, payload.Path);
        Assert.Equal("post", payload.Method);
        Assert.True(payload.Authorize);
        Assert.Equal(PolicyWorkflow.PolicyName, payload.Policy);
        Assert.Equal(SecondStageProtectedWorkflow.Timeout, payload.RequestTimeout);
        Assert.Equal(SecondStageProtectedWorkflow.SizeLimit, payload.RequestSizeLimit);
    }

    [Fact(DisplayName = "A bookmark persisted without the options is still enforced from the activity's current options")]
    public async Task ResumeBookmark_PersistedWithDefaultOptions_IsEnforcedFromTheActivity()
    {
        var workflow = new PolicyWorkflow();
        await PostAsync(workflow.StartPath, user: "admin");

        // Simulate a bookmark created by an earlier version, which dropped the endpoint options.
        var bookmark = Assert.Single(await FindBookmarksAsync(workflow.ResumePath));
        var legacy = bookmark.GetPayload<HttpEndpointBookmarkPayload>();
        bookmark.Payload = new HttpEndpointBookmarkPayload(legacy.Path, legacy.Method);
        await _app.Services.GetRequiredService<IBookmarkStore>().SaveAsync(bookmark);
        var persisted = Assert.Single(await FindBookmarksAsync(workflow.ResumePath)).GetPayload<HttpEndpointBookmarkPayload>();
        Assert.NotEqual(true, persisted.Authorize);

        Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsync(workflow.ResumePath, user: null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsync(workflow.ResumePath, user: "viewer")).StatusCode);

        // The rejected requests did not consume the wait: a permitted caller can still resume.
        Assert.Single(await FindBookmarksAsync(workflow.ResumePath));
        var response = await PostAsync(workflow.ResumePath, user: "admin");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("done", await response.Content.ReadAsStringAsync());
    }

    private async Task AssertResumeAsync(TwoStageHttpWorkflow workflow, string? user, HttpStatusCode expected)
    {
        // The start is always performed by an admin so that the resume is the only variable.
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(workflow.StartPath, user: "admin")).StatusCode);

        var response = await PostAsync(workflow.ResumePath, user);
        Assert.Equal(expected, response.StatusCode);

        if (expected == HttpStatusCode.OK)
        {
            Assert.Equal("done", await response.Content.ReadAsStringAsync());
            return;
        }

        // A rejected request leaves the workflow waiting, and a permitted caller can still resume it.
        Assert.Single(await FindBookmarksAsync(workflow.ResumePath));
        var retry = await PostAsync(workflow.ResumePath, user: "admin");
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal("done", await retry.Content.ReadAsStringAsync());
    }

    private Task<HttpResponseMessage> PostAsync(string path, string? user)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/workflows/{path}") { Content = new StringContent("{}") };
        if (user != null)
            request.Headers.Add(UserHeader, user);
        return _client.SendAsync(request);
    }

    private async Task<List<StoredBookmark>> FindBookmarksAsync(string path)
    {
        var hasher = _app.Services.GetRequiredService<IStimulusHasher>();
        var hash = hasher.Hash(HttpStimulusNames.HttpEndpoint, new HttpEndpointBookmarkPayload("/" + path, "post"));
        var store = _app.Services.GetRequiredService<IBookmarkStore>();
        return (await store.FindManyAsync(new BookmarkFilter { Hash = hash })).ToList();
    }

    private class HeaderAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(UserHeader, out var user))
                return Task.FromResult(AuthenticateResult.NoResult());

            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, user.ToString()), new Claim(ClaimTypes.Role, user.ToString())], "Test");
            return Task.FromResult(AuthenticateResult.Success(new(new(identity), "Test")));
        }
    }
}
