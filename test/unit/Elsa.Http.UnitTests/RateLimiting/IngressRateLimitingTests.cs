using System.Net;
using System.Threading.RateLimiting;
using Elsa.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TUnit.AspNetCore;

namespace Elsa.Http.UnitTests.RateLimiting;

public sealed class IngressRateLimitingTestEntryPoint;

public class IngressRateLimitingTests
{
    private const string PolicyName = "test";

    [Test]
    public async Task UseWorkflowsApiRateLimiting_AppliesPolicyToApiPrefix()
    {
        await using var factory = CreateRoutedApp(app => app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName));
        using var client = factory.CreateClient();

        using var firstResponse = await client.GetAsync("/elsa/api/ping");
        using var secondResponse = await client.GetAsync("/elsa/api/ping");

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task UseWorkflowsRateLimiting_AppliesPolicyToHttpWorkflowBasePath()
    {
        await using var factory = CreateApp(app => app.UseWorkflowsRateLimiting("/workflows", PolicyName));
        using var client = factory.CreateClient();

        using var firstResponse = await client.GetAsync("/workflows/hello-world");
        using var secondResponse = await client.GetAsync("/workflows/hello-world");

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    [Test]
    [Arguments("/workflows/")]
    [Arguments("workflows")]
    public async Task UseWorkflowsRateLimiting_NormalizesHttpWorkflowBasePath(string basePath)
    {
        await using var factory = CreateApp(app => app.UseWorkflowsRateLimiting(basePath, PolicyName));
        using var client = factory.CreateClient();

        using var firstResponse = await client.GetAsync("/workflows/hello-world");
        using var secondResponse = await client.GetAsync("/workflows/hello-world");

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task UseWorkflowsApiRateLimiting_NormalizesRoutePrefixWhitespace()
    {
        await using var factory = CreateRoutedApp(app => app.UseWorkflowsApiRateLimiting(" elsa/api ", PolicyName));
        using var client = factory.CreateClient();

        using var firstResponse = await client.GetAsync("/elsa/api/ping");
        using var secondResponse = await client.GetAsync("/elsa/api/ping");

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task UseWorkflowsApiRateLimiting_DoesNotApplyWhitespaceOnlyRoutePrefixToAllPaths()
    {
        await using var factory = CreateApp(app => app.UseWorkflowsApiRateLimiting("   ", PolicyName));
        using var client = factory.CreateClient();

        using var firstResponse = await client.GetAsync("/other/path");
        using var secondResponse = await client.GetAsync("/other/path");

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task UseWorkflowsApiRateLimiting_DoesNotApplyPolicyToOtherPaths()
    {
        await using var factory = CreateRoutedApp(app => app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName));
        using var client = factory.CreateClient();

        using var firstRateLimitedResponse = await client.GetAsync("/elsa/api/ping");
        using var secondRateLimitedResponse = await client.GetAsync("/elsa/api/ping");
        using var otherResponse = await client.GetAsync("/other/path");

        await Assert.That(otherResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UseWorkflowsRateLimiting_DoesNotApplyPolicyToOtherPaths()
    {
        await using var factory = CreateApp(app => app.UseWorkflowsRateLimiting("/workflows", PolicyName));
        using var client = factory.CreateClient();

        using var firstRateLimitedResponse = await client.GetAsync("/workflows/hello-world");
        using var secondRateLimitedResponse = await client.GetAsync("/workflows/hello-world");
        using var otherResponse = await client.GetAsync("/other/path");

        await Assert.That(otherResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    [Arguments("/")]
    [Arguments(" / ")]
    public async Task UseWorkflowsRateLimiting_DoesNotApplyRootBasePathToAllPaths(string basePath)
    {
        await using var factory = CreateApp(app => app.UseWorkflowsRateLimiting(basePath, PolicyName));
        using var client = factory.CreateClient();

        using var firstResponse = await client.GetAsync("/other/path");
        using var secondResponse = await client.GetAsync("/other/path");

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task UseWorkflowsRateLimiting_AppliesPolicyToMiddlewarePathWhenEndpointRoutesExist()
    {
        await using var factory = CreateAppWithEndpointRoute(app => app.UseWorkflowsRateLimiting("/workflows", PolicyName));
        using var client = factory.CreateClient();

        using var firstResponse = await client.GetAsync("/workflows/hello-world");
        using var secondResponse = await client.GetAsync("/workflows/hello-world");

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task UseRateLimitingPolicyForPath_DefaultOverloadRequiresMatchedEndpoint()
    {
        await using var factory = CreateAppWithEndpointRoute(app => app.UseRateLimitingPolicyForPath("/proxy", PolicyName, "Proxy rate limiting endpoint"));
        using var client = factory.CreateClient();

        using var firstResponse = await client.GetAsync("/proxy/downstream");
        using var secondResponse = await client.GetAsync("/proxy/downstream");

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task UseWorkflowsApiRateLimiting_UsesExistingGlobalRateLimiterMiddleware()
    {
        var policy = new CountingRateLimiterPolicy();
        await using var factory = CreateRoutedApp(
            app => app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName),
            options => options.AddPolicy(PolicyName, policy));
        using var client = factory.CreateClient();
        var partitionRequestCount = policy.PartitionRequestCount;

        using var firstResponse = await client.GetAsync("/elsa/api/ping");

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(policy.PartitionRequestCount > partitionRequestCount).IsTrue();

        partitionRequestCount = policy.PartitionRequestCount;
        using var secondResponse = await client.GetAsync("/elsa/api/ping");

        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
        await Assert.That(policy.PartitionRequestCount > partitionRequestCount).IsTrue();
    }

    [Test]
    public async Task UseWorkflowsApiRateLimiting_PreservesRoutedEndpointExecution()
    {
        await using var factory = CreateRoutedApp(app => app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName));
        using var client = factory.CreateClient();

        using var firstResponse = await client.GetAsync("/elsa/api/ping");
        var content = await firstResponse.Content.ReadAsStringAsync();
        using var secondResponse = await client.GetAsync("/elsa/api/ping");

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(content).IsEqualTo("pong");
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task UseWorkflowsApiRateLimiting_PreservesUnmatchedApiPrefixRouting()
    {
        await using var factory = CreateRoutedApp(app => app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName));
        using var client = factory.CreateClient();

        using var unmatchedResponse = await client.GetAsync("/elsa/api/not-found");
        using var routedResponse = await client.GetAsync("/elsa/api/ping");

        await Assert.That(unmatchedResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(routedResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task UseWorkflowsRateLimiting_PreservesEndpointRoutingNotFoundForUnmatchedPath()
    {
        await using var factory = CreateEndpointRoutedApp(app => app.UseWorkflowsRateLimiting("/workflows", PolicyName));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/workflows/not-found");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UseWorkflowsApiRateLimiting_CachesAugmentedRouteEndpointAndPreservesRouteDetails()
    {
        Endpoint? originalEndpoint = null;
        Endpoint? firstAugmentedEndpoint = null;
        Endpoint? secondAugmentedEndpoint = null;
        var requestCount = 0;
        var routeMetadata = new TestRouteMetadata("ping");
        await using var factory = new IngressRateLimitingWebApplicationFactory(
            services =>
            {
                services.AddRouting();
                AddRateLimiterServices(services);
            },
            app =>
            {
                app.UseRouting();
                app.Use(async (context, next) =>
                {
                    originalEndpoint ??= context.GetEndpoint();
                    await next(context);
                });
                app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName);
                app.Use(async (context, next) =>
                {
                    if (requestCount++ == 0)
                        firstAugmentedEndpoint = context.GetEndpoint();
                    else
                        secondAugmentedEndpoint = context.GetEndpoint();

                    await next(context);
                });
                app.UseRateLimiter();
                app.UseEndpoints(endpoints => endpoints
                    .MapGet("/elsa/api/ping", () => "pong")
                    .WithDisplayName("Elsa API Ping")
                    .WithMetadata(routeMetadata));
            });
        using var client = factory.CreateClient();

        using var firstResponse = await client.GetAsync("/elsa/api/ping");
        using var secondResponse = await client.GetAsync("/elsa/api/ping");

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
        var original = (await Assert.That(originalEndpoint).IsTypeOf<RouteEndpoint>())!;
        var firstAugmented = (await Assert.That(firstAugmentedEndpoint).IsTypeOf<RouteEndpoint>())!;
        var secondAugmented = (await Assert.That(secondAugmentedEndpoint).IsTypeOf<RouteEndpoint>())!;
        await Assert.That(firstAugmented).IsNotSameReferenceAs(original);
        await Assert.That(secondAugmented).IsSameReferenceAs(firstAugmented);
        await Assert.That(firstAugmented.RoutePattern.RawText).IsEqualTo(original.RoutePattern.RawText);
        await Assert.That(firstAugmented.Order).IsEqualTo(original.Order);
        await Assert.That(firstAugmented.DisplayName).IsEqualTo(original.DisplayName);
        await Assert.That(firstAugmented.Metadata.GetMetadata<TestRouteMetadata>()).IsSameReferenceAs(routeMetadata);
        await Assert.That(firstAugmented.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName).IsEqualTo(PolicyName);
    }

    [Test]
    public async Task UseWorkflowsApiRateLimiting_ReplacesExistingRateLimitingMetadata()
    {
        Endpoint? augmentedEndpoint = null;
        await using var factory = new IngressRateLimitingWebApplicationFactory(
            services =>
            {
                services.AddRouting();
                AddRateLimiterServices(services);
            },
            app =>
            {
                app.UseRouting();
                app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName);
                app.Use(async (context, next) =>
                {
                    augmentedEndpoint = context.GetEndpoint();
                    await next(context);
                });
                app.UseRateLimiter();
                app.UseEndpoints(endpoints => endpoints
                    .MapGet("/elsa/api/ping", () => "pong")
                    .RequireRateLimiting("other")
                    .DisableRateLimiting());
            });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/elsa/api/ping");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var augmented = (await Assert.That(augmentedEndpoint).IsTypeOf<RouteEndpoint>())!;
        var enableRateLimitingMetadata = await Assert.That(augmented.Metadata.OfType<EnableRateLimitingAttribute>()).HasSingleItem();
        await Assert.That(enableRateLimitingMetadata.PolicyName).IsEqualTo(PolicyName);
        await Assert.That(augmented.Metadata).DoesNotContain(x => x is DisableRateLimitingAttribute);
    }

    [Test]
    public async Task UseWorkflowsApiRateLimiting_FailsWhenPolicyIsNotRegistered()
    {
        await using var factory = CreateRoutedApp(
            app => app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName),
            options => AddFixedWindowLimiter(options, "other"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            using var client = factory.CreateClient();
            using var response = await client.GetAsync("/elsa/api/ping");
        });
    }

    [Test]
    public async Task UseWorkflowsApiRateLimiting_UsesFrameworkServiceValidation()
    {
        await using var factory = CreateApp(
            app => app.UseWorkflowsApiRateLimiting("elsa/api", PolicyName),
            registerRateLimiter: false);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            using var client = factory.CreateClient();
        });
    }

    private static IngressRateLimitingWebApplicationFactory CreateRoutedApp(
        Action<IApplicationBuilder> configure,
        Action<RateLimiterOptions>? configureRateLimiter = null)
    {
        return new(
            services =>
            {
                services.AddRouting();
                AddRateLimiterServices(services, configureRateLimiter);
            },
            app =>
            {
                app.UseRouting();
                configure(app);
                app.UseRateLimiter();
                app.UseEndpoints(endpoints => endpoints.MapGet("/elsa/api/ping", () => "pong"));
            });
    }

    private static IngressRateLimitingWebApplicationFactory CreateAppWithEndpointRoute(Action<IApplicationBuilder> configure)
    {
        return new(
            services =>
            {
                services.AddRouting();
                AddRateLimiterServices(services);
            },
            app =>
            {
                app.UseRouting();
                configure(app);
                app.UseRateLimiter();
                app.Run(context => context.Response.WriteAsync("ok"));
                app.UseEndpoints(endpoints => endpoints.MapGet("/elsa/api/ping", () => "pong"));
            });
    }

    private static IngressRateLimitingWebApplicationFactory CreateEndpointRoutedApp(Action<IApplicationBuilder> configure)
    {
        return new(
            services =>
            {
                services.AddRouting();
                AddRateLimiterServices(services);
            },
            app =>
            {
                app.UseRouting();
                configure(app);
                app.UseRateLimiter();
                app.UseEndpoints(endpoints => endpoints.MapGet("/elsa/api/ping", () => "pong"));
            });
    }

    private static IngressRateLimitingWebApplicationFactory CreateApp(
        Action<IApplicationBuilder> configure,
        Action<RateLimiterOptions>? configureRateLimiter = null,
        bool registerRateLimiter = true)
    {
        return new(
            services =>
            {
                services.AddRouting();
                if (registerRateLimiter)
                    AddRateLimiterServices(services, configureRateLimiter);
            },
            app =>
            {
                configure(app);
                app.UseRateLimiter();
                app.Run(context => context.Response.WriteAsync("ok"));
            });
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

    private sealed class IngressRateLimitingWebApplicationFactory(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configurePipeline) : TestWebApplicationFactory<IngressRateLimitingTestEntryPoint>
    {
        protected override IHostBuilder CreateHostBuilder() => new HostBuilder();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder
                .UseEnvironment(Environments.Production)
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices(configureServices)
                .Configure(configurePipeline);
        }
    }

    private sealed class CountingRateLimiterPolicy : IRateLimiterPolicy<string>
    {
        private int _partitionRequestCount;

        public int PartitionRequestCount => Volatile.Read(ref _partitionRequestCount);

        public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

        public RateLimitPartition<string> GetPartition(HttpContext httpContext)
        {
            Interlocked.Increment(ref _partitionRequestCount);
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
