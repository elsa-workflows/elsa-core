using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Retention.Contracts;

/// <summary>
/// Represents a pipeline of retention filters to execute sequentially, until a filter returns true.
/// </summary>
public interface IRetentionFilterPipeline
{
    /// <summary>
    /// Adds a filter to the end of the pipeline. 
    /// </summary>
    void AddFilter(Func<IServiceProvider, IRetentionFilter> filterFactory);
    
    /// <summary>
    /// Adds a filter to the end of the pipeline. 
    /// </summary>
    void AddFilter(Func<IRetentionFilter> filterFactory);
    
    /// <summary>
    /// Adds a filter to the end of the pipeline. 
    /// </summary>
    void AddFilter(IRetentionFilter filter);
    
    /// <summary>
    /// Adds a filter to the end of the pipeline.
    /// </summary>
    void AddFilter(Type filterType);

    /// <summary>
    /// Adds a filter to the end of the pipeline.
    /// </summary>
    void AddFilter<T>() where T : IRetentionFilter;

    /// <summary>
    /// Returns a filtered list of workflow instances that need to be deleted.
    /// </summary>
    Task<IEnumerable<WorkflowInstance>> FilterAsync(IEnumerable<WorkflowInstance> workflowInstances, CancellationToken cancellationToken = default);
}