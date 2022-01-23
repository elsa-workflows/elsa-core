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
        /// Gets a single workflow blueprint with the specified ID for the specified tenant and version.
        /// </summary>
        Task<IWorkflowBlueprint?> FindAsync(string definitionId, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a single workflow blueprint with the specified name for the specified tenant and version.
        /// </summary>
        Task<IWorkflowBlueprint?> FindByNameAsync(string name, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a single workflow blueprint with the specified tag for the specified tenant and version.
        /// </summary>
        Task<IWorkflowBlueprint?> FindByTagAsync(string tag, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a list of <see cref="WorkflowBlueprint"/> objects for the specified set of workflow definition version IDs.
        /// </summary>
        Task<IEnumerable<IWorkflowBlueprint>> FindByDefinitionVersionIds(IEnumerable<string> definitionVersionIds, CancellationToken cancellationToken);
    }
}