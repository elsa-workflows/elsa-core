using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Connections.Services;
using Elsa.Tenants;
using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionRuntimeHostPolicyTests(PostgreSqlConnectionsFixture fixture)
{
    private readonly AdmissionRuntimeTestFixture _runtime = new(fixture);

    [Fact]
    public async Task MappedHttpRouteRejectsHostBeforeHandlerOrWorkflowEffects()
    {
        var handlerCalls = 0;
        var route = new RouteEndpoint(_ => { handlerCalls++; return Task.CompletedTask; },
            RoutePatternFactory.Parse("/fixture-forbidden"), 0, EndpointMetadataCollection.Empty, "fixture-forbidden");
        var routes = new FixtureRoutes(route);
        await RejectHostAsync(services => services.AddSingleton<EndpointDataSource>(routes),
            "HTTP routes are unsupported in the isolated admission execution host.", async probe =>
            {
                Assert.Single(routes.Endpoints);
                Assert.Equal(0, handlerCalls);
                await ObserveAsync("host-route", nameof(MappedHttpRouteRejectsHostBeforeHandlerOrWorkflowEffects), "default",
                    new() { ["validationAttempts"] = 1, ["mappedRoutes"] = routes.Endpoints.Count,
                        ["handlerCalls"] = handlerCalls, ["activityEffects"] = probe.Count("activityEffects") });
            });
    }

    [Theory]
    [InlineData("host-forbidden-api", "api")]
    [InlineData("host-forbidden-alterations", "alterations")]
    public async Task ForbiddenEntrypointRegistrationRejectsBeforeFactoryInvocation(string caseId, string scenario)
    {
        var factoryCalls = 0;
        var type = scenario == "api" ? typeof(Elsa.Workflows.Api.Features.WorkflowsApiFeature) : typeof(Elsa.Alterations.Features.AlterationsFeature);
        await RejectHostAsync(services => services.AddSingleton(type, _ =>
            {
                factoryCalls++;
                throw new InvalidOperationException("fixture-forbidden-factory-invoked");
            }), "The isolated admission host contains an unsupported management, alteration, HTTP or distributed entrypoint.", async probe =>
            {
                Assert.Equal(0, factoryCalls);
                await ObserveAsync(caseId, nameof(ForbiddenEntrypointRegistrationRejectsBeforeFactoryInvocation), caseId,
                    new() { ["validationAttempts"] = 1, ["factoryCalls"] = factoryCalls, ["activityEffects"] = probe.Count("activityEffects") });
            });
    }

    [Theory]
    [InlineData("host-contributor-workflow", "workflow")]
    [InlineData("host-contributor-activity", "activity")]
    public async Task UnsupportedContributorRejectsBeforeConstructorAndCallback(string caseId, string scenario)
    {
        var counters = new ContributionCounters();
        await RejectHostAsync(services =>
            {
                if (scenario == "workflow")
                {
                    services.AddScoped<IWorkflowExecutionPipelineContributor>(_ => new WorkflowContributor(counters));
                }
                else
                {
                    services.AddScoped<IActivityExecutionPipelineContributor>(_ => new ActivityContributor(counters));
                }
            }, "Execution pipeline contributors are unsupported by the fixed admission host.", async probe =>
            {
                Assert.Equal(0, counters.Constructors);
                Assert.Equal(0, counters.Callbacks);
                await ObserveAsync(caseId, nameof(UnsupportedContributorRejectsBeforeConstructorAndCallback), caseId,
                    new() { ["validationAttempts"] = 1, ["constructorCalls"] = counters.Constructors,
                        ["callbackCalls"] = counters.Callbacks, ["activityEffects"] = probe.Count("activityEffects") });
            });
    }

    [Theory]
    [InlineData("host-unaudited-registry", "registry")]
    [InlineData("host-unaudited-storage", "storage")]
    public async Task UnauditedDependencyRejectsBeforeExecutionOperations(string caseId, string scenario)
    {
        var counters = new ContributionCounters();
        await RejectHostAsync(services =>
            {
                if (scenario == "registry")
                {
                    services.Replace(ServiceDescriptor.Scoped<IExecutionCycleRegistry>(_ => new UnauditedRegistry(counters)));
                }
                else
                {
                    services.AddScoped<IStorageDriver>(_ => new UnauditedStorage(counters));
                }
            }, "The actual admission preparation/notification chain contains an unaudited service.", async probe =>
            {
                Assert.True(counters.Constructors > 0);
                Assert.Equal(0, counters.Callbacks);
                await ObserveAsync(caseId, nameof(UnauditedDependencyRejectsBeforeExecutionOperations), caseId,
                    new() { ["validationAttempts"] = 1, ["dependencyResolved"] = counters.Constructors > 0,
                        ["executionOperations"] = counters.Callbacks, ["activityEffects"] = probe.Count("activityEffects") });
            });
    }

    [Theory]
    [InlineData("host-factory-classic", false)]
    [InlineData("host-factory-shell", true)]
    public async Task OriginalPipelineFactoriesAreIgnoredByValidatedExecutingHost(string caseId, bool shell)
    {
        await fixture.ResetSchemaAsync();
        var probe = new AdmissionRuntimeProbe(fixture.ConnectionString);
        var workflowFactories = 0;
        var activityFactories = 0;
        var setupCallbacks = 0;
        await using var services = AdmissionRuntimeHost.CreateServices(fixture.ConnectionString, probe, shell: shell,
            configureBeforeAdmission: registrations =>
            {
                registrations.AddScoped<IWorkflowExecutionPipeline>(provider =>
                {
                    workflowFactories++;
                    return new WorkflowExecutionPipeline(provider, _ => setupCallbacks++);
                });
                registrations.AddScoped<IActivityExecutionPipeline>(provider =>
                {
                    activityFactories++;
                    return new ActivityExecutionPipeline(provider, _ => setupCallbacks++);
                });
            });
        using var tenant = AdmissionRuntimeHost.EnterTenant(services);
        await AdmissionRuntimeHost.MigrateAsync(services);
        await AdmissionRuntimeHost.BootstrapAsync(services);
        var durableEffectsBefore = await probe.ReadDurableCountAsync("activityEffects");
        var execution = services.GetRequiredService<AdmissionExecutionService>();
        var admission = await execution.AdmitAsync(AdmissionWorkerHost.Event());
        Assert.Equal(AdmissionOutcome.Committed, admission.Outcome);
        Assert.Equal(WorkflowSubStatus.Finished, (await execution.ExecuteAsync(admission.AdmissionId!))!.SubStatus);
        var record = (await services.GetRequiredService<IAdmissionStore>().FindAsync(admission.AdmissionId!))!;
        Assert.Equal(AdmissionState.Terminal, record.State);
        Assert.False(record.AuthorityOutstanding);
        Assert.Equal(1, probe.Count("activityEffects"));
        var durableEffectsAfter = await probe.ReadDurableCountAsync("activityEffects");
        Assert.Equal(durableEffectsBefore + 1, durableEffectsAfter);
        Assert.Equal(0, workflowFactories);
        Assert.Equal(0, activityFactories);
        Assert.Equal(0, setupCallbacks);
        await ObserveAsync(caseId, nameof(OriginalPipelineFactoriesAreIgnoredByValidatedExecutingHost), caseId,
            new() { ["workflowFactoryCalls"] = workflowFactories, ["activityFactoryCalls"] = activityFactories,
                ["setupCallbacks"] = setupCallbacks, ["activityEffects"] = probe.Count("activityEffects"),
                ["durableActivityEffectsDelta"] = durableEffectsAfter - durableEffectsBefore, ["terminal"] = record.State == AdmissionState.Terminal });
    }

    [Theory]
    [InlineData("host-default-use-human", ConnectionUseKind.Human)]
    [InlineData("host-default-use-background", ConnectionUseKind.BackgroundSystem)]
    public async Task DefaultConnectionUseAuthorizationDeniesAuthenticatedCaller(string caseId, ConnectionUseKind kind)
    {
        await _runtime.RunAsync(async host =>
        {
            var principal = AuthenticatedPrincipal();
            Assert.True(principal.Identity!.IsAuthenticated);
            var authorizer = Assert.IsType<DenyAllConnectionUseAuthorizer>(host.Services.GetRequiredService<IConnectionUseAuthorizer>());
            var authorized = await authorizer.AuthorizeAsync(new(principal, kind,
                AdmissionWorkerHost.TenantId, AdmissionWorkerHost.EnvironmentId, "fixture-connection", "fixture-use"));
            Assert.False(authorized);
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            await ObserveAsync(caseId, nameof(DefaultConnectionUseAuthorizationDeniesAuthenticatedCaller), caseId,
                new() { ["authorizationAttempts"] = 1, ["authenticated"] = principal.Identity.IsAuthenticated,
                    ["authorized"] = authorized, ["activityEffects"] = host.Probe.Count("activityEffects") });
        });
    }

    [Fact]
    public async Task ResolvedConnectionLifecycleFacadesDenyEveryOperationWithoutChangingAdmission()
    {
        await _runtime.RunAsync(async host =>
        {
            var principal = AuthenticatedPrincipal();
            var lifecycle = host.Services.GetRequiredService<IConnectionLifecycleService>();
            var apiKeys = host.Services.GetRequiredService<IStaticApiKeyLifecycleService>();
            var recovery = host.Services.GetRequiredService<IConnectionLifecycleRecoveryService>();
            var background = host.Services.GetRequiredService<IConnectionBackgroundUseService>();
            var tenant = AdmissionWorkerHost.TenantId;
            var environment = AdmissionWorkerHost.EnvironmentId;
            const string connection = "fixture-connection";
            const string generation = "fixture-generation";
            var before = (await host.Store.FindAsync(host.AdmissionId))!;
            Func<Task>[] operations =
            [
                () => lifecycle.ConnectAsync(principal, new(tenant, environment, "fixture-provider", "fixture-account", new CredentialMaterial("synthetic-access", "synthetic-refresh", AdmissionWorkerHost.Now))),
                () => lifecycle.ResolveForUseAsync(principal, tenant, environment, connection),
                () => lifecycle.RefreshAsync(principal, tenant, environment, connection),
                () => lifecycle.DisconnectAsync(principal, tenant, environment, connection),
                () => lifecycle.RequestTokenRevocationAsync(principal, tenant, environment, connection, generation),
                () => lifecycle.RequestInstallationUninstallAsync(principal, tenant, environment, connection),
                () => lifecycle.CleanupGenerationAsync(principal, tenant, environment, connection, generation),
                () => apiKeys.ConnectApiKeyAsync(principal, new(tenant, environment, "fixture-provider", "fixture-account", "synthetic-api-key")),
                () => apiKeys.ReplaceApiKeyAsync(principal, tenant, environment, connection, 1, "synthetic-api-key"),
                () => recovery.ReconcileAsync(tenant, environment, connection),
                () => recovery.ReconcileOffboardingAsync(tenant, environment, connection),
                () => recovery.RefreshAsync(tenant, environment, connection),
                () => recovery.CleanupGenerationAsync(tenant, environment, connection, generation),
                () => background.ResolveForUseAsync(tenant, environment, connection)
            ];
            var denied = 0;
            foreach (var operation in operations)
            {
                var error = await Assert.ThrowsAsync<InvalidOperationException>(operation);
                Assert.Equal("Connection lifecycle operations require separate supported withdrawal and activation authority.", error.Message);
                denied++;
            }
            var after = (await host.Store.FindAsync(host.AdmissionId))!;
            Assert.Equal(14, denied);
            Assert.Equal(before.Revision, after.Revision);
            Assert.Equal(before.State, after.State);
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            await ObserveAsync("host-lifecycle-denied", nameof(ResolvedConnectionLifecycleFacadesDenyEveryOperationWithoutChangingAdmission), "default",
                new() { ["operationAttempts"] = operations.Length, ["deniedOperations"] = denied,
                    ["revisionUnchanged"] = before.Revision == after.Revision, ["stateUnchanged"] = before.State == after.State,
                    ["activityEffects"] = host.Probe.Count("activityEffects") });
        });
    }

    [Fact]
    public async Task RegisteredRealTenantStoreAllowsReadsButDeniesAllLifecycleMutations()
    {
        await fixture.ResetSchemaAsync();
        var inner = new MemoryTenantStore(new MemoryStore<Tenant>());
        var original = new Tenant { Id = AdmissionWorkerHost.TenantId, Name = "fixture-original" };
        await inner.AddAsync(original);
        var probe = new AdmissionRuntimeProbe(fixture.ConnectionString);
        await using var services = AdmissionRuntimeHost.CreateServices(fixture.ConnectionString, probe,
            configureBeforeAdmission: registrations => registrations.AddSingleton<ITenantStore>(inner));
        using var tenant = AdmissionRuntimeHost.EnterTenant(services);
        await services.GetRequiredService<AdmissionHostConfiguration>().ValidateAsync(services);
        var store = services.GetRequiredService<ITenantStore>();
        Assert.NotSame(inner, store);
        Assert.Same(original, await store.FindAsync(original.Id));
        Assert.Same(original, await store.FindAsync(TenantFilter.ById(original.Id)));
        Assert.Same(original, Assert.Single(await store.FindManyAsync(TenantFilter.ById(original.Id))));
        Assert.Same(original, Assert.Single(await store.ListAsync()));
        Func<Task>[] mutations =
        [
            () => store.AddAsync(new Tenant { Id = "fixture-new", Name = "fixture-new" }),
            () => store.UpdateAsync(new Tenant { Id = original.Id, Name = "fixture-changed" }),
            () => store.DeleteAsync(original.Id),
            () => store.DeleteAsync(TenantFilter.ById(original.Id))
        ];
        var denied = 0;
        foreach (var mutation in mutations)
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(mutation);
            Assert.Equal("Tenant lifecycle mutation is unsupported without admission withdrawal coordination.", error.Message);
            denied++;
        }
        var retained = Assert.Single(await inner.ListAsync());
        Assert.Same(original, retained);
        Assert.Equal("fixture-original", retained.Name);
        Assert.Equal(4, denied);
        Assert.Equal(0, probe.Count("activityEffects"));
        await ObserveAsync("host-tenant-mutations", nameof(RegisteredRealTenantStoreAllowsReadsButDeniesAllLifecycleMutations), "default",
            new() { ["readOperations"] = 4, ["mutationAttempts"] = mutations.Length, ["deniedMutations"] = denied,
                ["tenantCount"] = (await inner.ListAsync()).Count(), ["originalRetained"] = ReferenceEquals(original, retained),
                ["activityEffects"] = probe.Count("activityEffects") });
    }

    private async Task RejectHostAsync(Action<IServiceCollection> configure, string expectedMessage, Func<AdmissionRuntimeProbe, Task> asserted)
    {
        await fixture.ResetSchemaAsync();
        var probe = new AdmissionRuntimeProbe(fixture.ConnectionString);
        await using var services = AdmissionRuntimeHost.CreateServices(fixture.ConnectionString, probe, configure: configure);
        using var tenant = AdmissionRuntimeHost.EnterTenant(services);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            services.GetRequiredService<AdmissionHostConfiguration>().ValidateAsync(services).AsTask());
        Assert.Equal(expectedMessage, error.Message);
        Assert.Equal(0, probe.Count("workflowExecuting"));
        Assert.Equal(0, probe.Count("workflowStarted"));
        Assert.Equal(0, probe.Count("activityEffects"));
        await asserted(probe);
    }

    private Task ObserveAsync(string caseId, string method, string parameterId, Dictionary<string, object> facts) =>
        AdmissionProofObservation.WriteAsync(fixture, caseId, GetType().FullName + "." + method, parameterId, [],
            new Dictionary<string, bool> { ["behaviorAssertionsPassed"] = true }, facts);

    private static ClaimsPrincipal AuthenticatedPrincipal() => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "fixture-operator")], "fixture"));

    private sealed class FixtureRoutes(Endpoint route) : EndpointDataSource
    {
        public override IReadOnlyList<Endpoint> Endpoints { get; } = [route];
        public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;
    }

    private sealed class ContributionCounters
    {
        public int Constructors;
        public int Callbacks;
    }

    private sealed class WorkflowContributor : IWorkflowExecutionPipelineContributor
    {
        private readonly ContributionCounters _counters;
        public WorkflowContributor(ContributionCounters counters)
        {
            _counters = counters;
            counters.Constructors++;
        }
        public void Configure(IWorkflowExecutionPipelineBuilder builder) => _counters.Callbacks++;
    }

    private sealed class ActivityContributor : IActivityExecutionPipelineContributor
    {
        private readonly ContributionCounters _counters;
        public ActivityContributor(ContributionCounters counters)
        {
            _counters = counters;
            counters.Constructors++;
        }
        public void Configure(IActivityExecutionPipelineBuilder builder) => _counters.Callbacks++;
    }

    private sealed class UnauditedRegistry : IExecutionCycleRegistry
    {
        private readonly ContributionCounters _counters;
        public UnauditedRegistry(ContributionCounters counters)
        {
            _counters = counters;
            counters.Constructors++;
        }
        public int ActiveCount { get { _counters.Callbacks++; throw new InvalidOperationException("fixture-unaudited-operation"); } }
        public ExecutionCycleHandle BeginCycle(string workflowInstanceId, string? ingressSourceName, CancellationToken linkedToken, Action? cancelCallback = null)
        {
            _counters.Callbacks++;
            throw new InvalidOperationException("fixture-unaudited-operation");
        }
        public IReadOnlyCollection<ExecutionCycleHandle> ListActiveCycles()
        {
            _counters.Callbacks++;
            throw new InvalidOperationException("fixture-unaudited-operation");
        }
    }

    private sealed class UnauditedStorage : IStorageDriver
    {
        private readonly ContributionCounters _counters;
        public UnauditedStorage(ContributionCounters counters)
        {
            _counters = counters;
            counters.Constructors++;
        }
        public double Priority => 0;
        public IEnumerable<string> Tags => [];
        public ValueTask WriteAsync(string id, object value, StorageDriverContext context)
        {
            _counters.Callbacks++;
            throw new InvalidOperationException("fixture-unaudited-operation");
        }
        public ValueTask<object?> ReadAsync(string id, StorageDriverContext context)
        {
            _counters.Callbacks++;
            throw new InvalidOperationException("fixture-unaudited-operation");
        }
        public ValueTask DeleteAsync(string id, StorageDriverContext context)
        {
            _counters.Callbacks++;
            throw new InvalidOperationException("fixture-unaudited-operation");
        }
    }
}
