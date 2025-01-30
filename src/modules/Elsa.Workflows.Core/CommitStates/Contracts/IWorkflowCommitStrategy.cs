using System.Text.Json.Serialization;
using Elsa.Workflows.CommitStates.Serialization;

namespace Elsa.Workflows.CommitStates;

[JsonConverter(typeof(WorkflowCommitStateStrategyJsonConverter))]
public interface IWorkflowCommitStrategy
{
    CommitAction ShouldCommit(WorkflowCommitStateStrategyContext context);
}