using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Elsa.Extensions;
using Elsa.Http.Bookmarks;
using Microsoft.AspNetCore.Http;
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
            .AddActivitiesFrom<SlowActivity>()
            .AddWorkflow<SecondStageProtectedWorkflow>()
            .AddWorkflow<MultiMethodWorkflow>()
            .AddWorkflow<AnonymousWorkflow>()
            .AddWorkflow<PolicyWithoutAuthorizeWorkflow>()
            .AddWorkflow<TimeoutWorkflow>());

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

    [Theory(DisplayName = "A bookmark persisted without the options is still enforced from the activity's current options")]
    [InlineData(null)]
    [InlineData(false)]
    public async Task ResumeBookmark_PersistedWithDefaultOptions_IsEnforcedFromTheActivity(bool? persistedAuthorize)
    {
        var workflow = new PolicyWorkflow();
        await PostAsync(workflow.StartPath, user: "admin");
        var original = Assert.Single(await MakeLegacyAsync(workflow.ResumePath, persistedAuthorize));

        Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsync(workflow.ResumePath, user: null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsync(workflow.ResumePath, user: "viewer")).StatusCode);

        // The rejected requests did not consume or replace the bookmark: a permitted caller can still resume.
        Assert.Equal(original, Assert.Single(await FindBookmarksAsync(workflow.ResumePath)).Id);
        var response = await PostAsync(workflow.ResumePath, user: "admin");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("done", await response.Content.ReadAsStringAsync());
        Assert.Empty(await FindBookmarksAsync(workflow.ResumePath));
    }

    [Fact(DisplayName = "A rejected resume on a legacy bookmark of a multi-method endpoint keeps one bookmark per method")]
    public async Task ResumeBookmark_LegacyMultiMethod_RejectedResumeDoesNotDuplicateBookmarks()
    {
        var workflow = new MultiMethodWorkflow();
        await PostAsync(workflow.StartPath, user: "admin");
        var getBefore = Assert.Single(await MakeLegacyAsync(workflow.ResumePath, false, HttpMethods.Get));
        var postBefore = Assert.Single(await MakeLegacyAsync(workflow.ResumePath, false, HttpMethods.Post));

        Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsync(workflow.ResumePath, user: null)).StatusCode);

        Assert.Equal(getBefore, Assert.Single(await FindBookmarksAsync(workflow.ResumePath, HttpMethods.Get)).Id);
        Assert.Equal(postBefore, Assert.Single(await FindBookmarksAsync(workflow.ResumePath, HttpMethods.Post)).Id);

        // The other method is not ambiguous, and a permitted caller can resume.
        var response = await SendAsync(HttpMethod.Get, workflow.ResumePath, user: "admin");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("done", await response.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "A burst of rejected resumes on a legacy bookmark does not duplicate it or fail")]
    public async Task ResumeBookmark_LegacyBurstOfRejectedResumes_DoesNotDuplicateBookmarks()
    {
        var workflow = new PolicyWorkflow();
        await PostAsync(workflow.StartPath, user: "admin");
        var original = Assert.Single(await MakeLegacyAsync(workflow.ResumePath, false));

        var statuses = await Task.WhenAll(Enumerable.Range(0, 12).Select(i => PostAsync(workflow.ResumePath, i % 3 == 0 ? "viewer" : null)).Select(async t => (await t).StatusCode));

        Assert.All(statuses, status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(original, Assert.Single(await FindBookmarksAsync(workflow.ResumePath)).Id);
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(workflow.ResumePath, user: "admin")).StatusCode);
    }

    [Fact(DisplayName = "A burst that includes a permitted caller on a legacy bookmark never fails with a server error")]
    public async Task ResumeBookmark_LegacyBurstWithPermittedCaller_DoesNotFail()
    {
        var workflow = new PolicyWorkflow();
        await PostAsync(workflow.StartPath, user: "admin");
        await MakeLegacyAsync(workflow.ResumePath, false);

        var users = Enumerable.Range(0, 12).Select(i => i == 6 ? "admin" : i % 3 == 0 ? "viewer" : null).ToList();
        var statuses = await Task.WhenAll(users.Select(async user => (await PostAsync(workflow.ResumePath, user)).StatusCode));

        Assert.All(statuses, status => Assert.True(status is HttpStatusCode.OK or HttpStatusCode.Unauthorized or HttpStatusCode.NotFound, $"Unexpected status {status}."));
        Assert.Contains(HttpStatusCode.OK, statuses);
        Assert.True((await FindBookmarksAsync(workflow.ResumePath)).Count <= 1);
    }

    [Fact(DisplayName = "An endpoint without Authorize accepts anonymous callers on start and resume")]
    public async Task Anonymous_StartAndResume_AreAllowed()
    {
        var workflow = new AnonymousWorkflow();
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(workflow.StartPath, user: null)).StatusCode);

        var response = await PostAsync(workflow.ResumePath, user: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("done", await response.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "A policy without Authorize is not enforced, on start and resume")]
    public async Task PolicyWithoutAuthorize_StartAndResume_AreNotEnforced()
    {
        var workflow = new PolicyWithoutAuthorizeWorkflow();
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(workflow.StartPath, user: null)).StatusCode);

        var response = await PostAsync(workflow.ResumePath, user: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("done", await response.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "A resume without an HTTP context is not authorized by the endpoint and keeps the workflow waiting")]
    public async Task Resume_WithoutHttpContext_CreatesCrossBoundaryBookmarkWithoutAuthorizing()
    {
        var workflow = new PolicyWorkflow();
        await PostAsync(workflow.StartPath, user: "admin");
        var bookmark = Assert.Single(await FindBookmarksAsync(workflow.ResumePath));

        using var scope = _app.Services.CreateScope();
        var resumer = scope.ServiceProvider.GetRequiredService<IWorkflowResumer>();
        await resumer.ResumeAsync(bookmark.Id, new Dictionary<string, object>());

        var remaining = await FindBookmarksAsync(workflow.ResumePath);
        Assert.Empty(remaining);
        var all = await _app.Services.GetRequiredService<IBookmarkStore>().FindManyAsync(new BookmarkFilter { WorkflowInstanceId = bookmark.WorkflowInstanceId });
        var crossBoundary = Assert.Single(all);
        Assert.Equal("true", crossBoundary.Metadata?[BookmarkMetadata.HttpCrossBoundaryMetadataKey]);
    }

    [Fact(DisplayName = "The request timeout of the endpoint is applied when the workflow is resumed")]
    public async Task Resume_AppliesTheRequestTimeout()
    {
        var workflow = new TimeoutWorkflow();
        await PostAsync(workflow.StartPath, user: null);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await PostAsync(workflow.ResumePath, user: null);
        }
        catch (OperationCanceledException)
        {
            // The cancellation surfaces to the caller once the timeout has elapsed.
        }

        Assert.True(stopwatch.Elapsed < TimeoutWorkflow.SlowActivityDuration / 2, $"The request took {stopwatch.Elapsed}.");
    }

    private async Task AssertResumeAsync(TwoStageHttpWorkflow workflow, string? user, HttpStatusCode expected)
    {
        // The start is always performed by an admin so that the resume is the only variable.
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(workflow.StartPath, user: "admin")).StatusCode);

        var bookmarkId = Assert.Single(await FindBookmarksAsync(workflow.ResumePath)).Id;
        var response = await PostAsync(workflow.ResumePath, user);
        Assert.Equal(expected, response.StatusCode);

        if (expected == HttpStatusCode.OK)
        {
            Assert.Equal("done", await response.Content.ReadAsStringAsync());
            return;
        }

        // A rejected request leaves the workflow waiting on the same bookmark, and a permitted caller can still resume it.
        Assert.Equal(bookmarkId, Assert.Single(await FindBookmarksAsync(workflow.ResumePath)).Id);
        var retry = await PostAsync(workflow.ResumePath, user: "admin");
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal("done", await retry.Content.ReadAsStringAsync());
    }

    private Task<HttpResponseMessage> PostAsync(string path, string? user) => SendAsync(HttpMethod.Post, path, user);

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? user)
    {
        var request = new HttpRequestMessage(method, $"/workflows/{path}");
        if (method == HttpMethod.Post)
            request.Content = new StringContent("{}");
        if (user != null)
            request.Headers.Add(UserHeader, user);
        return _client.SendAsync(request);
    }

    private async Task<List<StoredBookmark>> FindBookmarksAsync(string path, string method = "POST")
    {
        var hasher = _app.Services.GetRequiredService<IStimulusHasher>();
        var hash = hasher.Hash(HttpStimulusNames.HttpEndpoint, new HttpEndpointBookmarkPayload("/" + path, method.ToLowerInvariant()));
        var store = _app.Services.GetRequiredService<IBookmarkStore>();
        return (await store.FindManyAsync(new BookmarkFilter { Hash = hash })).ToList();
    }

    /// <summary>
    /// Rewrites the persisted bookmarks to look like the ones created by earlier versions, which dropped the endpoint options.
    /// Returns the ids of the rewritten bookmarks for the method.
    /// </summary>
    private async Task<List<string>> MakeLegacyAsync(string path, bool? authorize, string method = "POST")
    {
        var store = _app.Services.GetRequiredService<IBookmarkStore>();
        var bookmarks = await FindBookmarksAsync(path, method);

        foreach (var bookmark in bookmarks)
        {
            var current = bookmark.GetPayload<HttpEndpointBookmarkPayload>();
            bookmark.Payload = new HttpEndpointBookmarkPayload(current.Path, current.Method, authorize);
            await store.SaveAsync(bookmark);
        }

        var persisted = await FindBookmarksAsync(path, method);
        Assert.All(persisted, x => Assert.NotEqual(true, x.GetPayload<HttpEndpointBookmarkPayload>().Authorize));
        Assert.Null(persisted.First().GetPayload<HttpEndpointBookmarkPayload>().Policy);
        return persisted.Select(x => x.Id).ToList();
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
