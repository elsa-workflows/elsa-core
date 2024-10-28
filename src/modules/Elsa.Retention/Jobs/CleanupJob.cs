using System.Diagnostics.CodeAnalysis;
using Elsa.Common.Models;
using Elsa.Retention.Contracts;
using Elsa.Retention.Options;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Retention.Jobs;

/// <summary>
///     Deletes all workflow instances that match any of the defined <see cref="IRetentionPolicy" />
/// </summary>
[SuppressMessage("Trimming", "IL2055:Either the type on which the MakeGenericType is called can\'t be statically determined, or the type parameters to be used for generic arguments can\'t be statically determined.")]
public class CleanupJob
{
    private readonly ILogger _logger;
    private readonly CleanupOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;

    /// <summary>
    ///     Creates a new cleanup job
    /// </summary>
    /// <param name="workflowInstanceStore"></param>
    /// <param name="options"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="logger"></param>
    public CleanupJob(
        IWorkflowInstanceStore workflowInstanceStore,
        IOptions<CleanupOptions> options,
        IServiceProvider serviceProvider,
        ILogger<CleanupJob> logger)
    {
        _workflowInstanceStore = workflowInstanceStore;
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    ///     Executes the cleanup job
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();

        IEnumerable<IRetentionPolicy> policies = scope.ServiceProvider.GetServices<IRetentionPolicy>();
        Dictionary<Type, object> collectors = GetServices(typeof(IRelatedEntityCollector), typeof(IRelatedEntityCollector<>));


        foreach (IRetentionPolicy policy in policies)
        {
            WorkflowInstanceFilter filter = policy.FilterFactory(scope.ServiceProvider).Build();
            PageArgs pageArgs = PageArgs.FromPage(0, _options.PageSize);

            long deletedWorkflowInstances = 0;

            while (true)
            {
                Page<WorkflowInstance> page = await _workflowInstanceStore.FindManyAsync(filter, pageArgs, cancellationToken);

                if (page.Items.Count == 0)
                {
                    break;
                }

                foreach (KeyValuePair<Type, object> collectorService in collectors)
                {
                    Type cleanupStrategyConcreteType = policy.CleanupStrategy.MakeGenericType(collectorService.Key);

                    IRelatedEntityCollector? collector = collectorService.Value as IRelatedEntityCollector;
                    ICleanupStrategy? cleanupService = _serviceProvider.GetService(cleanupStrategyConcreteType) as ICleanupStrategy;

                    if (collector == null)
                    {
                        _logger.LogWarning("Failed to collect entities of type {Type}", collectorService.Key.Name);
                        continue;
                    }

                    if (cleanupService == null)
                    {
                        _logger.LogWarning("Failed to clean up {Type} no clean up strategy found that implements {CleanupType}", collectorService.Key.Name, policy.CleanupStrategy.Name);
                        continue;
                    }

                    await foreach (ICollection<object> entities in collector.GetRelatedEntitiesGeneric(page.Items).WithCancellation(cancellationToken))
                    {
                        await cleanupService.Cleanup(entities);
                    }
                }

                deletedWorkflowInstances += await _workflowInstanceStore.DeleteAsync(new WorkflowInstanceFilter
                {
                    Ids = page.Items.Select(x => x.Id).ToArray()
                }, cancellationToken);

                if (page.TotalCount <= page.Items.Count + pageArgs.Offset)
                {
                    break;
                }
            }

            _logger.LogInformation("Cleaned up {WorkflowInstanceCount} workflow instances through {Policy}", deletedWorkflowInstances, policy.Name);
        }
    }

    private Dictionary<Type, object> GetServices(Type baseType, Type openType)
    {
        IEnumerable<object?> services = _serviceProvider.GetServices(baseType);

        return services
            .Where(x => x?.GetType() != null)
            .Select(service => new
            {
                Service = service,
                GenericArgument = service!.GetType()
                    .GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == openType)?
                    .GetGenericArguments()
                    .FirstOrDefault()
            })
            .Where(x => x.GenericArgument != null)
            .ToDictionary(x => x.GenericArgument!, x => x.Service)!;
    }
}