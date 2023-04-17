using Elsa.ProtoActor.Grains;
using Elsa.ProtoActor.Protos;
using Elsa.Workflows.Core.State;

namespace Elsa.ProtoActor;

internal record WorkflowSnapshot(string DefinitionId, string InstanceId, int Version, WorkflowState WorkflowState, IDictionary<string, object>? Input);

internal record WorkflowRegistrySnapshot(ICollection<WorkflowInstanceEntry> Entries);