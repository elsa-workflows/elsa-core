using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities.Flowchart.Contracts;

/// <summary>
/// Gives implementing activities a chance to customize certain flowchart execution behaviors.
/// </summary>
public interface IJoinNode : IActivity
{
    bool GetShouldExecute(FlowJoinContext context);
}

public record FlowJoinContext(ActivityExecutionContext ActivityExecutionContext, FlowScope Scope, Activities.Flowchart Flowchart, IActivity Activity, ICollection<IActivity> InboundActivities);