using Elsa.ProtoActor.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.ProtoActor;

internal record WorkflowSnapshot(string DefinitionId, string InstanceId, int Version, WorkflowState WorkflowState, Dictionary<string, object>? Input);

internal record RunningWorkflowsSnapshot(List<RunningWorkflowInstanceEntry> Entries);