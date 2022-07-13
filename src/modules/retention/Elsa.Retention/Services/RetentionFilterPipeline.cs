using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Retention.Contracts;
using Elsa.Retention.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Retention.Services;

public class RetentionFilterPipeline : IRetentionFilterPipeline
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IList<Func<IServiceProvider, IRetentionFilter>> _filterFactories = new List<Func<IServiceProvider, IRetentionFilter>>();
    public RetentionFilterPipeline(IServiceScopeFactory serviceScopeFactory) => _serviceScopeFactory = serviceScopeFactory;

    public void AddFilter(Func<IServiceProvider, IRetentionFilter> filterFactory) => _filterFactories.Add(filterFactory);
    public void AddFilter(Func<IRetentionFilter> filterFactory) => AddFilter(_ => filterFactory());
    public void AddFilter(IRetentionFilter filter) => AddFilter(() => filter);
    public void AddFilter(Type filterType) => AddFilter(sp => (IRetentionFilter)ActivatorUtilities.GetServiceOrCreateInstance(sp, filterType));
    public void AddFilter<T>() where T : IRetentionFilter => AddFilter(typeof(T));

    public async Task<IEnumerable<WorkflowInstance>> FilterAsync(IEnumerable<WorkflowInstance> workflowInstances, CancellationToken cancellationToken) =>
        await FilterInternalAsync(workflowInstances, cancellationToken).ToListAsync(cancellationToken);

    private async IAsyncEnumerable<WorkflowInstance> FilterInternalAsync(IEnumerable<WorkflowInstance> workflowInstances, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var filters = _filterFactories.Select(x => x(scope.ServiceProvider)).ToList();

        foreach (var workflowInstance in workflowInstances)
            if (await GetShouldDeleteAsync(filters, workflowInstance, cancellationToken))
                yield return workflowInstance;
    }

    private static async Task<bool> GetShouldDeleteAsync(IEnumerable<IRetentionFilter> filters, WorkflowInstance workflowInstance, CancellationToken cancellationToken)
    {
        var context = new RetentionFilterContext(workflowInstance, cancellationToken);

        foreach (var filter in filters)
            if (await filter.GetShouldDeleteAsync(context))
                return true;

        return false;
    }
}