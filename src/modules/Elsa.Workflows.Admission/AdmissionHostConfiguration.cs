using Elsa.Connections.Contracts;
using Elsa.Connections.Services;
using Elsa.Tenants;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Admission;

/// <summary>
/// Explicit reviewed configuration for an isolated worker host. Concrete preparation services,
/// notifications, activity providers and fixed workflow activity types must be audited before activation.
/// This is a trusted extension contract, not a sandbox for hostile plugins or concurrent graph mutation.
/// </summary>
public sealed class AdmissionHostConfiguration
{
    private readonly HashSet<Type> _auditedTypes;
    private readonly HashSet<Type> _activityTypes;
    private readonly Dictionary<string, string> _subscriptions;
    private IServiceCollection? _registrations;

    public AdmissionHostConfiguration(string tenantId, string environmentId, IEnumerable<Type> auditedServiceTypes, IEnumerable<Type> allowedActivityTypes,
        IEnumerable<AdmissionSubscriptionConfiguration> allowedSubscriptions)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(environmentId) || tenantId.Length > 256 || environmentId.Length > 256)
        {
            throw new ArgumentException("The isolated admission host requires explicit bounded tenant and environment bindings.");
        }
        TenantId = tenantId;
        EnvironmentId = environmentId;
        _auditedTypes = auditedServiceTypes.ToHashSet();
        _activityTypes = allowedActivityTypes.ToHashSet();
        _subscriptions = new(StringComparer.Ordinal);
        foreach (var subscription in allowedSubscriptions)
        {
            subscription.Validate();
            if (subscription.TenantId != tenantId || subscription.EnvironmentId != environmentId ||
                !_subscriptions.TryAdd(subscription.Id, subscription.ConfigurationFingerprint))
            {
                throw new ArgumentException("The predefined subscription allowlist must have unique identities in this host scope.");
            }
        }
        if (_auditedTypes.Count == 0 || _activityTypes.Count == 0 || _subscriptions.Count == 0)
        {
            throw new ArgumentException("The supported preparation chain and workflow activity allowlist must be explicitly reviewed.");
        }
    }

    public string TenantId { get; }
    public string EnvironmentId { get; }
    internal void BindRegistrations(IServiceCollection registrations)
    {
        if (_registrations != null && !ReferenceEquals(_registrations, registrations))
        {
            throw new InvalidOperationException("An admission host configuration cannot be shared by multiple execution hosts.");
        }
        _registrations = registrations;
    }

    public ValueTask ValidateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Inspect registrations before resolving any contributor: arbitrary constructors and
        // callbacks must not run as a side effect of deciding that a host is unsupported.
        if (_registrations == null || _registrations.Any(x => x.ServiceType == typeof(IWorkflowExecutionPipelineContributor) ||
            x.ServiceType == typeof(IActivityExecutionPipelineContributor)))
        {
            throw new InvalidOperationException("Execution pipeline contributors are unsupported by the fixed admission host.");
        }
        services.GetRequiredService<AdmissionExecutionComposition>().Validate(services);
        if (services.GetRequiredService<IWorkflowRuntime>().GetType() != typeof(AdmissionWorkflowRuntime) ||
            services.GetRequiredService<IWorkflowDispatcher>().GetType() != typeof(AdmissionWorkflowDispatcher) ||
            services.GetRequiredService<IWorkflowInstanceVariableManager>().GetType() != typeof(AdmissionWorkflowInstanceVariableManager) ||
            services.GetRequiredService<IWorkflowRunner>().GetType() != typeof(WorkflowRunner) ||
            services.GetRequiredService<IWorkflowExecutionPipeline>().GetType() != typeof(WorkflowExecutionPipeline) ||
            services.GetRequiredService<IAdmissionExecutionDataReader>().GetType() != typeof(AdmissionExecutionDataReader) ||
            services.GetRequiredService<IWorkflowExecutionGuard>().GetType() != typeof(AdmissionExecutionGuard))
        {
            throw new InvalidOperationException("Admission requires the reviewed built-in runner, pipeline and isolated Local runtime adapter.");
        }
        if (services.GetRequiredService<IWorkflowDefinitionPublisher>().GetType() != typeof(AdmissionDeniedManagement) ||
            services.GetRequiredService<IWorkflowDefinitionManager>().GetType() != typeof(AdmissionDeniedManagement) ||
            services.GetRequiredService<IConnectionLifecycleService>().GetType() != typeof(AdmissionDeniedConnectionManagement) ||
            services.GetRequiredService<IStaticApiKeyLifecycleService>().GetType() != typeof(AdmissionDeniedConnectionManagement) ||
            services.GetRequiredService<IConnectionLifecycleRecoveryService>().GetType() != typeof(AdmissionDeniedConnectionManagement) ||
            services.GetRequiredService<IConnectionBackgroundUseService>().GetType() != typeof(AdmissionDeniedConnectionManagement) ||
            _registrations.Any(x => x.ServiceType == typeof(DefaultConnectionLifecycleService)) ||
            (services.GetService<ITenantStore>() is { } tenantStore && tenantStore.GetType() != typeof(AdmissionTenantStore)))
        {
            throw new InvalidOperationException("An unsupported direct management/lifecycle service remains invocable.");
        }
        foreach (var descriptor in _registrations)
        {
            var types = new[] { descriptor.ServiceType, descriptor.ImplementationType, descriptor.ImplementationInstance?.GetType(), descriptor.ImplementationFactory?.Method.DeclaringType };
            if (types.Where(x => x != null).Any(x => Forbidden(x!)))
            {
                throw new InvalidOperationException("The isolated admission host contains an unsupported management, alteration, HTTP or distributed entrypoint.");
            }
        }
        // First supported host is a worker, with no mapped HTTP endpoints. This checks the
        // actual route data source too; excluding only a client wrapper is insufficient.
        if (services.GetService<EndpointDataSource>() is { Endpoints.Count: > 0 })
        {
            throw new InvalidOperationException("HTTP routes are unsupported in the isolated admission execution host.");
        }
        var required = new[]
        {
            typeof(IWorkflowDefinitionService), typeof(IWorkflowGraphBuilder), typeof(IWorkflowInstanceManager),
            typeof(IWorkflowStateExtractor), typeof(IWorkflowStateSerializer), typeof(IActivitySerializer), typeof(IPayloadSerializer),
            typeof(IActivityRegistry), typeof(IActivityRegistryLookupService), typeof(IMaterializerRegistry),
            typeof(IActivitySchedulerFactory), typeof(IActivityInvoker), typeof(IStorageDriverManager), typeof(IExecutionCycleRegistry),
            typeof(ILoggerStateGenerator<ActivityExecutionContext>),
            typeof(ILoggerStateGenerator<WorkflowExecutionContext>), typeof(INotificationSender), typeof(ICommitStateHandler),
            typeof(IBookmarksPersister), typeof(IVariablePersistenceManager), typeof(IWorkflowCommitTransaction), typeof(IWorkflowCommitNotificationBuffer)
        };
        foreach (var contract in required)
        {
            DemandAudited(services.GetRequiredService(contract).GetType());
        }
        foreach (var driver in services.GetServices<IStorageDriver>())
        {
            DemandAudited(driver.GetType());
        }
        foreach (var observer in services.GetServices<IAdmissionExecutionObserver>())
        {
            DemandAudited(observer.GetType());
        }
        foreach (var provider in services.GetServices<IActivityProvider>())
        {
            DemandAudited(provider.GetType());
        }
        foreach (var handler in services.GetServices<INotificationHandler>())
        {
            DemandAudited(handler.GetType());
        }
        var materializers = services.GetRequiredService<IMaterializerRegistry>();
        if (materializers.GetMaterializer(JsonWorkflowMaterializer.MaterializerName)?.GetType() != typeof(JsonWorkflowMaterializer))
        {
            throw new InvalidOperationException("Only the audited JSON materializer is supported for admission bootstrap.");
        }
        return ValueTask.CompletedTask;
    }

    internal void ValidateSubscription(AdmissionSubscriptionConfiguration configuration)
    {
        if (configuration.TenantId != TenantId || configuration.EnvironmentId != EnvironmentId ||
            !_subscriptions.TryGetValue(configuration.Id, out var fingerprint) || fingerprint != configuration.ConfigurationFingerprint)
        {
            throw new InvalidOperationException("The trusted subscription is not a predefined binding in this isolated host.");
        }
    }

    internal void ValidateGraph(WorkflowGraph graph)
    {
        foreach (var node in graph.Nodes)
        {
            if (!_activityTypes.Contains(node.Activity.GetType()) || node.Activity.GetCanStartWorkflow())
            {
                throw new InvalidOperationException("The predefined admission workflow contains an unaudited activity or autonomous start trigger.");
            }
        }
    }
    private void DemandAudited(Type type)
    {
        if (!_auditedTypes.Contains(type))
        {
            throw new InvalidOperationException("The actual admission preparation/notification chain contains an unaudited service.");
        }
    }

    // The opt-in friend adapter can check additional actual services against the SAME immutable
    // host audit inventory. It cannot add or change accepted types or invoke execution authority.
    internal void DemandAuditedServiceType(Type type) => DemandAudited(type);
    private static bool Forbidden(Type type)
    {
        var name = type.FullName ?? "";
        return name.StartsWith("Elsa.Workflows.Api.", StringComparison.Ordinal) || name.StartsWith("Elsa.Alterations.", StringComparison.Ordinal) ||
            name.StartsWith("Elsa.Workflows.Runtime.ProtoActor.", StringComparison.Ordinal) || name.StartsWith("Elsa.Http.", StringComparison.Ordinal) ||
            name.StartsWith("Elsa.Tenants.Endpoints.", StringComparison.Ordinal);
    }
}
