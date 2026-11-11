using Elsa.Common.Multitenancy;
using Elsa.Common.Multitenancy.EventHandlers;
using Elsa.Common.RecurringTasks;
using Elsa.Http;
using Elsa.Tenants.AspNetCore;
using Elsa.Tenants.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Tenants.UnitTests.Services;

/// <summary>
/// Tests that the resolver pipeline reads tenants from the tenant service registry instead of listing them on every request.
/// </summary>
public class TenantResolutionLookupTests
{
    [Theory]
    [InlineData(10)]
    [InlineData(10_000)]
    public async Task InvokePipelineAsync_ResolvingById_DoesNotListTenantsPerRequest(int tenantCount)
    {
        // Arrange
        var provider = new CountingTenantsProvider(CreateTenants(tenantCount));
        await using var fixture = new Fixture(provider);
        var invoker = fixture.CreateInvoker(Resolved("tenant-7"));
        await invoker.InvokePipelineAsync(); // The first call loads the registry.
        var providerListCallsAfterWarmUp = provider.ListCalls;
        fixture.TenantServiceSpy.ClearReceivedCalls();

        // Act
        Tenant? tenant = null;
        for (var request = 0; request < 50; request++)
            tenant = await invoker.InvokePipelineAsync();

        // Assert
        Assert.Equal("tenant-7", tenant?.Id);
        Assert.Equal(providerListCallsAfterWarmUp, provider.ListCalls);
        await fixture.TenantServiceSpy.DidNotReceive().ListAsync(Arg.Any<CancellationToken>());
        await fixture.TenantServiceSpy.DidNotReceive().ListAsync(Arg.Any<TenantFilter>(), Arg.Any<CancellationToken>());
        await fixture.TenantServiceSpy.Received(50).FindAsync("tenant-7", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokePipelineAsync_WithLegacyNullTenantId_ResolvesItAsTheDefaultTenant()
    {
        // Arrange
        var legacyTenant = new Tenant { Id = null!, Name = "Legacy" };
        await using var fixture = new Fixture(new CountingTenantsProvider([legacyTenant, new Tenant { Id = "tenant-1", Name = "Tenant 1" }]));

        // Act
        var tenant = await fixture.CreateInvoker(Resolved("")).InvokePipelineAsync();

        // Assert
        Assert.Same(legacyTenant, tenant);
    }

    [Fact]
    public async Task InvokePipelineAsync_WithDifferentCasing_DoesNotResolve()
    {
        // Arrange
        await using var fixture = new Fixture(new CountingTenantsProvider([new Tenant { Id = "tenant-a", Name = "Tenant A" }]));

        // Act
        var tenant = await fixture.CreateInvoker(Resolved("Tenant-A")).InvokePipelineAsync();

        // Assert
        Assert.Null(tenant);
    }

    [Fact]
    public async Task InvokePipelineAsync_WhenProviderIsEmpty_ResolvesTheDefaultTenant()
    {
        // Arrange
        await using var fixture = new Fixture(new CountingTenantsProvider([]));

        // Act
        var tenant = await fixture.CreateInvoker(Resolved("")).InvokePipelineAsync();

        // Assert
        Assert.Same(Tenant.Default, tenant);
    }

    [Fact]
    public async Task InvokePipelineAsync_AfterTenantIsAddedAndRefreshed_ResolvesIt()
    {
        // Arrange
        var provider = new CountingTenantsProvider(CreateTenants(2));
        await using var fixture = new Fixture(provider);
        var invoker = fixture.CreateInvoker(Resolved("new-tenant"));
        Assert.Null(await invoker.InvokePipelineAsync());
        provider.Tenants.Add(new Tenant { Id = "new-tenant", Name = "New" });

        // Act & Assert: an edit made directly in the store is not visible until the registry is refreshed.
        Assert.Null(await invoker.InvokePipelineAsync());
        await fixture.TenantService.RefreshAsync();
        Assert.Equal("new-tenant", (await invoker.InvokePipelineAsync())?.Id);
    }

    [Fact]
    public async Task InvokePipelineAsync_AfterTenantIsRemovedAndRefreshed_NoLongerResolvesIt()
    {
        // Arrange
        var provider = new CountingTenantsProvider(CreateTenants(3));
        await using var fixture = new Fixture(provider);
        var invoker = fixture.CreateInvoker(Resolved("tenant-1"));
        Assert.Equal("tenant-1", (await invoker.InvokePipelineAsync())?.Id);
        provider.Tenants.RemoveAll(x => x.Id == "tenant-1");

        // Act & Assert
        Assert.Equal("tenant-1", (await invoker.InvokePipelineAsync())?.Id);
        await fixture.TenantService.RefreshAsync();
        Assert.Null(await invoker.InvokePipelineAsync());
    }

    [Fact]
    public async Task HostTenantResolver_ResolvesTheTenantWithTheMatchingHostAndPort()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            CreateTenant("tenant-a", ("Http:Host", "a.example.com")),
            CreateTenant("tenant-b", ("Http:Host", "b.example.com:5000"))
        };
        await using var fixture = new Fixture(new CountingTenantsProvider(tenants));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("b.example.com", 5000);
        var resolver = new HostTenantResolver(new HttpContextAccessor { HttpContext = httpContext });

        // Act
        var tenant = await fixture.CreateInvoker(resolver).InvokePipelineAsync();

        // Assert
        Assert.Equal("tenant-b", tenant?.Id);
    }

    [Fact]
    public async Task HostTenantResolver_WithUnknownHost_DoesNotResolve()
    {
        // Arrange
        await using var fixture = new Fixture(new CountingTenantsProvider([CreateTenant("tenant-a", ("Http:Host", "a.example.com"))]));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("unknown.example.com");
        var resolver = new HostTenantResolver(new HttpContextAccessor { HttpContext = httpContext });

        // Act
        var tenant = await fixture.CreateInvoker(resolver).InvokePipelineAsync();

        // Assert
        Assert.Null(tenant);
    }

    [Fact]
    public async Task RoutePrefixTenantResolver_ResolvesTheTenantWithTheMatchingPrefix_WithoutListingFromTheProvider()
    {
        // Arrange: a tenant without a prefix must not get in the way.
        var provider = new CountingTenantsProvider([new Tenant { Id = "", Name = "Default" }, CreateTenant("tenant-a", ("Http:Prefix", "acme"))]);
        await using var fixture = new Fixture(provider);
        var newRequest = () => fixture.CreateInvoker(CreatePrefixResolver(fixture, "acme", new CapturingLogger()));
        await newRequest().InvokePipelineAsync(); // The first call loads the registry.
        var providerListCallsAfterWarmUp = provider.ListCalls;

        // Act
        var tenant = await newRequest().InvokePipelineAsync();

        // Assert
        Assert.Equal("tenant-a", tenant?.Id);
        Assert.Equal(providerListCallsAfterWarmUp, provider.ListCalls);
    }

    [Fact]
    public async Task RoutePrefixTenantResolver_WithNoMatchingPrefix_DoesNotResolve()
    {
        // Arrange
        await using var fixture = new Fixture(new CountingTenantsProvider([new Tenant { Id = "", Name = "Default" }, CreateTenant("tenant-a", ("Http:Prefix", "acme"))]));

        // Act
        var tenant = await fixture.CreateInvoker(CreatePrefixResolver(fixture, "other", new CapturingLogger())).InvokePipelineAsync();

        // Assert
        Assert.Null(tenant);
    }

    [Fact]
    public async Task RoutePrefixTenantResolver_WithDuplicatePrefixes_DoesNotResolveAndLogsAWarning()
    {
        // Arrange
        var logger = new CapturingLogger();
        await using var fixture = new Fixture(new CountingTenantsProvider([CreateTenant("tenant-a", ("Http:Prefix", "acme")), CreateTenant("tenant-b", ("Http:Prefix", "acme"))]));

        // Act
        var tenant = await fixture.CreateInvoker(CreatePrefixResolver(fixture, "acme", logger)).InvokePipelineAsync();

        // Assert
        Assert.Null(tenant);
        var warning = Assert.Single(logger.Warnings);
        Assert.Contains("acme", warning);
        Assert.Contains("tenant-a", warning);
        Assert.Contains("tenant-b", warning);
    }

    [Fact]
    public async Task InvokePipelineAsync_AfterTenantHostAndPrefixChangeUnderTheSameId_ResolvesTheNewValuesOnly()
    {
        // Arrange: the provider returns new instances on every call, like an EF-backed store.
        var provider = new CountingTenantsProvider([CreateTenant("tenant-a", ("Http:Host", "old.example.com"), ("Http:Prefix", "old"))]);
        await using var fixture = new Fixture(provider);
        Task<Tenant?> ResolveByHost(string host) => fixture.CreateInvoker(CreateHostResolver(host)).InvokePipelineAsync();
        Task<Tenant?> ResolveByPrefix(string prefix) => fixture.CreateInvoker(CreatePrefixResolver(fixture, prefix, new CapturingLogger())).InvokePipelineAsync();
        Assert.Equal("tenant-a", (await ResolveByHost("old.example.com"))?.Id);
        provider.Tenants[0] = CreateTenant("tenant-a", ("Http:Host", "new.example.com"), ("Http:Prefix", "new"));

        // Act
        await fixture.TenantService.RefreshAsync();

        // Assert
        Assert.Equal("tenant-a", (await ResolveByHost("new.example.com"))?.Id);
        Assert.Equal("tenant-a", (await ResolveByPrefix("new"))?.Id);
        Assert.Null(await ResolveByHost("old.example.com"));
        Assert.Null(await ResolveByPrefix("old"));
    }

    private static HostTenantResolver CreateHostResolver(string host)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host);
        return new HostTenantResolver(new HttpContextAccessor { HttpContext = httpContext });
    }

    private static RoutePrefixTenantResolver CreatePrefixResolver(Fixture fixture, string prefix, CapturingLogger logger)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = $"/{prefix}/workflows";
        var routeMatcher = Substitute.For<IRouteMatcher>();
        routeMatcher.Match(Arg.Any<string>(), Arg.Any<string>()).Returns(new RouteValueDictionary { ["tenantPrefix"] = prefix });
        var routeTable = Substitute.For<IRouteTable>();
        routeTable.GetEnumerator().Returns(_ => new List<HttpRouteData> { new("{tenantPrefix}/workflows") }.GetEnumerator());
        var serviceProvider = new ServiceCollection()
            .AddSingleton(routeMatcher)
            .AddSingleton(routeTable)
            .AddSingleton(fixture.TenantService)
            .AddSingleton<ILogger<RoutePrefixTenantResolver>>(logger)
            .BuildServiceProvider();

        // The resolver is scoped, so each request gets a new instance.
        return new RoutePrefixTenantResolver(new HttpContextAccessor { HttpContext = httpContext }, serviceProvider);
    }

    private sealed class CapturingLogger : ILogger<RoutePrefixTenantResolver>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
                Warnings.Add(formatter(state, exception));
        }
    }

    private static ITenantResolver Resolved(string tenantId)
    {
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync(Arg.Any<TenantResolverContext>()).Returns(TenantResolverResult.Resolved(tenantId));
        return resolver;
    }

    private static List<Tenant> CreateTenants(int count) =>
        Enumerable.Range(0, count).Select(i => new Tenant { Id = $"tenant-{i}", Name = $"Tenant {i}" }).ToList();

    private static Tenant CreateTenant(string id, params (string Key, string Value)[] configuration) => new()
    {
        Id = id,
        Name = id,
        Configuration = new ConfigurationBuilder().AddInMemoryCollection(configuration.Select(x => new KeyValuePair<string, string?>(x.Key, x.Value))).Build()
    };

    private sealed class CountingTenantsProvider(List<Tenant> tenants) : ITenantsProvider
    {
        public List<Tenant> Tenants { get; } = tenants;
        public int ListCalls { get; private set; }

        public Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default)
        {
            ListCalls++;
            return Task.FromResult<IEnumerable<Tenant>>(Tenants.ToList());
        }

        public Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(filter.Apply(Tenants.AsQueryable()).FirstOrDefault());
    }

    /// <summary>
    /// A real <see cref="DefaultTenantService"/> over the given provider, with a spy in front of it so tests can see which calls the invoker makes.
    /// </summary>
    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        public Fixture(ITenantsProvider provider)
        {
            _serviceProvider = new ServiceCollection()
                .AddSingleton(provider)
                .AddSingleton<ITenantScopeFactory, DefaultTenantScopeFactory>()
                .AddSingleton<ITenantAccessor, DefaultTenantAccessor>()
                .AddSingleton(Substitute.For<ITenantActivatedEvent>())
                .AddSingleton(Substitute.For<ITenantDeactivatedEvent>())
                .AddSingleton(Substitute.For<ITenantDeletedEvent>())
                .AddSingleton(Substitute.For<IAllTenantsActivatedEvent>())
                .AddSingleton<TenantEventsManager>()
                .AddSingleton<RecurringTaskScheduleManager>()
                .AddSingleton<ITenantService, DefaultTenantService>()
                .AddOptions()
                .AddLogging()
                .BuildServiceProvider();
            TenantService = _serviceProvider.GetRequiredService<ITenantService>();
            TenantServiceSpy = CreateSpy(TenantService);
        }

        public ITenantService TenantService { get; }
        public ITenantService TenantServiceSpy { get; }

        public DefaultTenantResolverPipelineInvoker CreateInvoker(ITenantResolver resolver)
        {
            var pipelineBuilder = Substitute.For<ITenantResolverPipelineBuilder>();
            pipelineBuilder.Build(Arg.Any<IServiceProvider>()).Returns([resolver]);
            var options = Microsoft.Extensions.Options.Options.Create(new MultitenancyOptions { TenantResolverPipelineBuilder = pipelineBuilder });

            return new(options, TenantServiceSpy, _serviceProvider, NullLogger<DefaultTenantResolverPipelineInvoker>.Instance);
        }

        public async ValueTask DisposeAsync()
        {
            if (TenantService is IAsyncDisposable disposable)
                await disposable.DisposeAsync();

            await _serviceProvider.DisposeAsync();
        }

        private static ITenantService CreateSpy(ITenantService inner)
        {
            var spy = Substitute.For<ITenantService>();
            spy.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => inner.FindAsync(call.Arg<string>(), call.Arg<CancellationToken>()));
            spy.ListAsync(Arg.Any<CancellationToken>()).Returns(call => inner.ListAsync(call.Arg<CancellationToken>()));
            return spy;
        }
    }
}
