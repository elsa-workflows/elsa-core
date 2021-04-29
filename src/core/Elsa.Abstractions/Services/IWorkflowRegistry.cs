using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Represents a complete store of all versions of all workflow blueprints. 
    /// </summary>
    public interface IWorkflowRegistry
    {
        /// <summary>
        /// Lists all versions of all workflow blueprints.
        /// </summary>
        Task<IEnumerable<IWorkflowBlueprint>> ListAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Lists only those workflow blueprints that are published or have at least one non-finished workflow.
        /// </summary>
        Task<IEnumerable<IWorkflowBlueprint>> ListActiveAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a single workflow blueprint with the specified ID for the specified tenant and version.
        /// </summary>
        Task<IWorkflowBlueprint?> GetAsync(string id, string? tenantId, VersionOptions version, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Returns all workflow blueprints that fit the specified predicate.
        /// </summary>
        Task<IEnumerable<IWorkflowBlueprint>> FindManyAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Returns a single workflow blueprint that fits the specified predicate. 
        /// </summary>
        Task<IWorkflowBlueprint?> FindAsync(Func<IWorkflowBlueprint, bool> predicate, CancellationToken cancellationToken = default);
    }
}