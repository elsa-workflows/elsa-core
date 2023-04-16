﻿using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Contracts
{
    /// <summary>
    /// Imports a workflow definition.
    /// </summary>
    public interface IWorkflowDefinitionImporter
    {
        /// <summary>
        /// Imports a workflow definition.
        /// </summary>
        /// <param name="request">A DTO representing the workflow definition to import.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The imported workflow definition.</returns>
        Task<WorkflowDefinition> ImportAsync(SaveWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
    }
}
