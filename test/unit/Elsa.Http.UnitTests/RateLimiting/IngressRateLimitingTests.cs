using System.Net;
using System.Threading.RateLimiting;
using Elsa.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
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
    public async Task UseWorkflowsRateLimiting_NormalizesHttpWorkflowBasePath()
    {
        await using var app = await CreateAppAsync(app => app.UseWorkflowsRateLimiting("/workflows/", PolicyName));
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

        var firstResponse = await client.GetAsync("/elsa/api/workflow-definitions");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(1, policy.PartitionRequestCount);

        var secondResponse = await client.GetAsync("/elsa/api/workflow-definitions");

        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
    }

    [Fact]
    public async Task UseWorkflowsApiRateLimiting_PreservesRoutedEndpointExecution()
    {
        await using var app = await CreateRoutedAppAsync(app => app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName));
        var client = app.GetTestClient();

        var firstResponse = await client.GetAsync("/elsa/api/ping");
        var content = await firstResponse.Content.ReadAsStringAsync();
        var secondResponse = await client.GetAsync("/elsa/api/ping");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal("pong", content);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
    }

    [Fact]
    public async Task UseWorkflowsApiRateLimiting_CachesAugmentedRouteEndpointAndPreservesRouteDetails()
    {
        var builder = CreateBuilder();
        AddRateLimiterServices(builder.Services);
        RouteEndpoint? originalEndpoint = null;
        RouteEndpoint? firstAugmentedEndpoint = null;
        RouteEndpoint? secondAugmentedEndpoint = null;
        var requestCount = 0;
        var routeMetadata = new TestRouteMetadata("ping");
        var app = new TestApplication(builder.Build(), app =>
        {
            app.MapGet("/elsa/api/ping", () => "pong")
                .WithDisplayName("Elsa API Ping")
                .WithMetadata(routeMetadata);
            app.UseRouting();
            app.Use(async (context, next) =>
            {
                originalEndpoint ??= Assert.IsType<RouteEndpoint>(context.GetEndpoint());
                await next(context);
            });
            app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName);
            app.Use(async (context, next) =>
            {
                var augmentedEndpoint = Assert.IsType<RouteEndpoint>(context.GetEndpoint());
                requestCount++;
                if (requestCount == 1)
                    firstAugmentedEndpoint = augmentedEndpoint;
                else
                    secondAugmentedEndpoint = augmentedEndpoint;

                await next(context);
            });
            app.UseRateLimiter();
        });
        await using (app)
        {
            app.Configure();
            await app.StartAsync();
            var client = app.GetTestClient();

            var firstResponse = await client.GetAsync("/elsa/api/ping");
            var secondResponse = await client.GetAsync("/elsa/api/ping");

            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
        }

        Assert.NotNull(originalEndpoint);
        Assert.NotNull(firstAugmentedEndpoint);
        Assert.NotNull(secondAugmentedEndpoint);
        Assert.NotSame(originalEndpoint, firstAugmentedEndpoint);
        Assert.Same(firstAugmentedEndpoint, secondAugmentedEndpoint);
        Assert.Equal(originalEndpoint.RoutePattern.RawText, firstAugmentedEndpoint.RoutePattern.RawText);
        Assert.Equal(originalEndpoint.Order, firstAugmentedEndpoint.Order);
        Assert.Equal(originalEndpoint.DisplayName, firstAugmentedEndpoint.DisplayName);
        Assert.Same(routeMetadata, firstAugmentedEndpoint.Metadata.GetMetadata<TestRouteMetadata>());
        Assert.Equal(PolicyName, firstAugmentedEndpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
    }

    [Fact]
    public async Task UseWorkflowsApiRateLimiting_ReplacesExistingRateLimitingMetadata()
    {
        var builder = CreateBuilder();
        AddRateLimiterServices(builder.Services);
        RouteEndpoint? augmentedEndpoint = null;
        var app = new TestApplication(builder.Build(), app =>
        {
            app.MapGet("/elsa/api/ping", () => "pong")
                .RequireRateLimiting("other")
                .DisableRateLimiting();
            app.UseRouting();
            app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName);
            app.Use(async (context, next) =>
            {
                augmentedEndpoint ??= Assert.IsType<RouteEndpoint>(context.GetEndpoint());
                await next(context);
            });
            app.UseRateLimiter();
        });
        await using (app)
        {
            app.Configure();
            await app.StartAsync();
            var client = app.GetTestClient();

            var response = await client.GetAsync("/elsa/api/ping");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        Assert.NotNull(augmentedEndpoint);
        var enableRateLimitingMetadata = augmentedEndpoint.Metadata.OfType<EnableRateLimitingAttribute>().ToList();
        Assert.Single(enableRateLimitingMetadata);
        Assert.Equal(PolicyName, enableRateLimitingMetadata.Single().PolicyName);
        Assert.DoesNotContain(augmentedEndpoint.Metadata, x => x is DisableRateLimitingAttribute);
    }

    [Fact]
    public async Task UseWorkflowsApiRateLimiting_FailsWhenPolicyIsNotRegistered()
    {
        using var app = CreateApp(
            app => app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName),
            options => AddFixedWindowLimiter(options, "other"));

        try
        {
            app.Configure();
        }
        catch (InvalidOperationException exception)
        {
            Assert.Contains($"Rate limiting policy '{PolicyName}' is not registered", exception.Message);
            Assert.DoesNotContain("services were not registered", exception.Message);
            return;
        }

        await app.StartAsync();
        var client = app.GetTestClient();
        HttpResponseMessage? response = null;
        var runtimeException = await Record.ExceptionAsync(async () => response = await client.GetAsync("/elsa/api/workflow-definitions"));

        if (runtimeException is InvalidOperationException invalidOperationException)
        {
            Assert.Contains(PolicyName, invalidOperationException.Message);
            return;
        }

        Assert.Null(runtimeException);
        Assert.Equal(HttpStatusCode.InternalServerError, response!.StatusCode);
    }

    [Fact]
    public void UseWorkflowsApiRateLimiting_ThrowsWhenRateLimiterIsNotRegistered()
    {
        using var app = CreateApp(
            app => app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName),
            registerRateLimiter: false);

        var exception = Assert.Throws<InvalidOperationException>(() => app.Configure());

        Assert.Contains("Rate limiting services are not registered", exception.Message);
    }

    private static async Task<TestApplication> CreateAppAsync(Action<WebApplication> configure, Action<RateLimiterOptions>? configureRateLimiter = null)
    {
        var app = CreateApp(configure, configureRateLimiter);
        app.Configure();
        await app.StartAsync();
        return app;
    }

    private static async Task<TestApplication> CreateRoutedAppAsync(Action<WebApplication> configure)
    {
        var builder = CreateBuilder();
        AddRateLimiterServices(builder.Services);
        var app = new TestApplication(builder.Build(), app =>
        {
            app.MapGet("/elsa/api/ping", () => "pong");
            app.UseRouting();
            configure(app);
            app.UseRateLimiter();
        });

        app.Configure();
        await app.StartAsync();
        return app;
    }

    private static TestApplication CreateApp(Action<WebApplication> configure, Action<RateLimiterOptions>? configureRateLimiter = null, bool registerRateLimiter = true)
    {
        var builder = CreateBuilder();

        if (registerRateLimiter)
            AddRateLimiterServices(builder.Services, configureRateLimiter);

        return new TestApplication(builder.Build(), app =>
        {
            configure(app);
            app.UseRateLimiter();
            app.Run(context => context.Response.WriteAsync("ok"));
        });
    }

    private static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        return builder;
    }

    private static void AddRateLimiterServices(IServiceCollection services, Action<RateLimiterOptions>? configureRateLimiter = null)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            if (configureRateLimiter == null)
                AddFixedWindowLimiter(options, PolicyName);
            else
                configureRateLimiter(options);
        });
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
        public void Configure() => configure(app);

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
            return RateLimitPartition.GetFixedWindowLimiter(PolicyName, _ => new()
            {
                PermitLimit = 1,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
        }
    }

    private sealed record TestRouteMetadata(string Value);
}
