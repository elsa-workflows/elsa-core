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
public class CleanupJob(
    IWorkflowInstanceStore workflowInstanceStore,
    IEnumerable<IRetentionPolicy> policies,
    IOptions<CleanupOptions> options,
    IServiceProvider serviceProvider,
    ILogger<CleanupJob> logger)
{
    private readonly ILogger _logger = logger;
    private readonly CleanupOptions _options = options.Value;

    /// <summary>
    ///     Executes the cleanup job
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine(DateTime.Now.ToLongTimeString());
        var collectors = GetServices(typeof(IRelatedEntityCollector), typeof(IRelatedEntityCollector<>));
        var deletedWorkflowInstances = 0L;
        
        foreach (var policy in policies)
        {
            var filter = policy.FilterFactory(serviceProvider).Build();
            var pageArgs = PageArgs.FromPage(0, _options.PageSize);

            while (true)
            {
                var page = await workflowInstanceStore.FindManyAsync(filter, pageArgs, cancellationToken);

                if (page.Items.Count == 0)
                {
                    break;
                }

                foreach (var collectorService in collectors)
                {
                    var cleanupStrategyConcreteType = policy.CleanupStrategy.MakeGenericType(collectorService.Key);
                    
                    if(cleanupStrategyConcreteType == typeof(WorkflowInstance))
                        continue;
                    
                    var collector = collectorService.Value as IRelatedEntityCollector;
                    var cleanupService = serviceProvider.GetService(cleanupStrategyConcreteType) as ICleanupStrategy;

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

                    await foreach (var entities in collector.GetRelatedEntitiesGeneric(page.Items).WithCancellation(cancellationToken))
                    {
                        await cleanupService.Cleanup(entities);
                    }
                }
                
                var cleanupWorkflowInstances = policy.CleanupStrategy.MakeGenericType(typeof(WorkflowInstance));
                var workflowInstanceCleaner = serviceProvider.GetService(cleanupWorkflowInstances) as ICleanupStrategy<WorkflowInstance>;

                if (workflowInstanceCleaner == null)
                    throw new Exception($"{policy.CleanupStrategy} has no strategy to clean WorkflowInstances");
                
                await workflowInstanceCleaner.Cleanup(page.Items);
                deletedWorkflowInstances += page.Items.Count;

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
        var services = serviceProvider.GetServices(baseType);

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