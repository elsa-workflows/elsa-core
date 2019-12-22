using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowRegistry
    {
        Task<IEnumerable<(WorkflowBlueprint, IActivity)>> ListByStartActivityAsync(
            string activityType,
            CancellationToken cancellationToken = default);

        Task<WorkflowBlueprint> GetWorkflowBlueprintAsync(
            string id,
            VersionOptions version,
            CancellationToken cancellationToken = default);
    }
}