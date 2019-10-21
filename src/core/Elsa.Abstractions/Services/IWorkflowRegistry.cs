using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IWorkflowRegistry
    {
        Task<IEnumerable<(WorkflowDefinitionVersion, ActivityDefinition)>> ListByStartActivityAsync(
            string activityType,
            CancellationToken cancellationToken = default);

        Task<WorkflowDefinitionVersion> GetWorkflowDefinitionAsync(
            string id,
            VersionOptions version,
            CancellationToken cancellationToken = default);
    }
}