using System.Net;
using System.Threading.RateLimiting;
using Elsa.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.UnitTests.RateLimiting;

public class IngressRateLimitingTests
{
    private const string PolicyName = "test";

    [Fact]
    public async Task UseWorkflowsApiRateLimiting_AppliesPolicyToApiPrefix()
    {
        await using var app = await CreateAppAsync(app => app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName));
        var client = app.GetTestClient();

        var firstResponse = await client.GetAsync("/elsa/api/workflow-definitions");
        var secondResponse = await client.GetAsync("/elsa/api/workflow-definitions");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
    }

    [Fact]
    public async Task UseWorkflowsRateLimiting_AppliesPolicyToHttpWorkflowBasePath()
    {
        await using var app = await CreateAppAsync(app => app.UseWorkflowsRateLimiting("/workflows", PolicyName));
        var client = app.GetTestClient();

        var firstResponse = await client.GetAsync("/workflows/hello-world");
        var secondResponse = await client.GetAsync("/workflows/hello-world");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
    }

    [Fact]
    public async Task UseWorkflowsApiRateLimiting_DoesNotApplyPolicyToOtherPaths()
    {
        await using var app = await CreateAppAsync(app => app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName));
        var client = app.GetTestClient();

        await client.GetAsync("/elsa/api/workflow-definitions");
        await client.GetAsync("/elsa/api/workflow-definitions");
        var otherResponse = await client.GetAsync("/other/path");

        Assert.Equal(HttpStatusCode.OK, otherResponse.StatusCode);
    }

    [Fact]
    public async Task UseWorkflowsRateLimiting_DoesNotApplyPolicyToOtherPaths()
    {
        await using var app = await CreateAppAsync(app => app.UseWorkflowsRateLimiting("/workflows", PolicyName));
        var client = app.GetTestClient();

        await client.GetAsync("/workflows/hello-world");
        await client.GetAsync("/workflows/hello-world");
        var otherResponse = await client.GetAsync("/other/path");

        Assert.Equal(HttpStatusCode.OK, otherResponse.StatusCode);
    }

    [Fact]
    public async Task UseWorkflowsApiRateLimiting_UsesExistingGlobalRateLimiterMiddleware()
    {
        var policy = new CountingRateLimiterPolicy();
        await using var app = await CreateAppAsync(
            app => app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName),
            options => options.AddPolicy(PolicyName, policy));
        var client = app.GetTestClient();

        var response = await client.GetAsync("/elsa/api/workflow-definitions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, policy.PartitionRequestCount);
    }

    [Fact]
    public void UseWorkflowsApiRateLimiting_ThrowsWhenPolicyIsNotRegistered()
    {
        using var app = CreateApp(
            app => app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName),
            options => AddFixedWindowLimiter(options, "other"));

        var exception = Assert.Throws<InvalidOperationException>(() => app.Configure());

        Assert.Contains($"Rate limiting policy '{PolicyName}' is not registered", exception.Message);
    }

    [Fact]
    public void UseWorkflowsApiRateLimiting_ThrowsWhenRateLimiterIsNotRegistered()
    {
        using var app = CreateApp(
            app => app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName),
            registerRateLimiter: false);

        var exception = Assert.Throws<InvalidOperationException>(() => app.Configure());

        Assert.Contains($"Rate limiting policy '{PolicyName}' is not registered", exception.Message);
    }

    private static async Task<TestApplication> CreateAppAsync(Action<WebApplication> configure, Action<RateLimiterOptions>? configureRateLimiter = null)
    {
        var app = CreateApp(configure, configureRateLimiter);
        app.Configure();
        await app.StartAsync();
        return app;
    }

    private static TestApplication CreateApp(Action<WebApplication> configure, Action<RateLimiterOptions>? configureRateLimiter = null, bool registerRateLimiter = true)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        if (registerRateLimiter)
        {
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                if (configureRateLimiter == null)
                    AddFixedWindowLimiter(options, PolicyName);
                else
                    configureRateLimiter(options);
            });
        }

        return new TestApplication(builder.Build(), configure);
    }

    private static void AddFixedWindowLimiter(RateLimiterOptions options, string policyName)
    {
        options.AddFixedWindowLimiter(policyName, limiterOptions =>
        {
            limiterOptions.PermitLimit = 1;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });
    }

    private sealed class TestApplication(WebApplication app, Action<WebApplication> configure) : IAsyncDisposable, IDisposable
    {
        public void Configure()
        {
            configure(app);
            app.UseRateLimiter();
            app.Run(context => context.Response.WriteAsync("ok"));
        }

        public HttpClient GetTestClient() => app.GetTestClient();

        public Task StartAsync() => app.StartAsync();

        public void Dispose() => app.DisposeAsync().AsTask().GetAwaiter().GetResult();

        public ValueTask DisposeAsync() => app.DisposeAsync();
    }

    private sealed class CountingRateLimiterPolicy : IRateLimiterPolicy<string>
    {
        public int PartitionRequestCount { get; private set; }

        public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

        public RateLimitPartition<string> GetPartition(HttpContext httpContext)
        {
            PartitionRequestCount++;
            return RateLimitPartition.GetNoLimiter(PolicyName);
        }
    }
}
