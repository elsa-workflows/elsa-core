using Elsa.Runtime.Protos;
using Elsa.Workflows.Core.State;

namespace Elsa.ProtoActor;

public record WorkflowSnapshot(string DefinitionId, int Version, WorkflowState WorkflowState, IDictionary<string, object>? Input);
public record BookmarkSnapshot(ICollection<StoredBookmark> Bookmarks);