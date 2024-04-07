using Elsa.Workflows.State;

namespace Elsa.ProtoActor.Snapshots;

internal record WorkflowInstanceGrainSnapshot(string DefinitionId, string InstanceId, int Version, WorkflowState WorkflowState, Dictionary<string, object>? Input);