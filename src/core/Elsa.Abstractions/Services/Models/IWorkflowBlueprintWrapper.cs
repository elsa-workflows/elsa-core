using System.Collections.Generic;

namespace Elsa.Services.Models
{
    public interface IWorkflowBlueprintWrapper
    {
        IWorkflowBlueprint WorkflowBlueprint { get; }
        IEnumerable<IActivityBlueprintWrapper> Activities { get; }
    }
}