using Elsa.Runtime.Protos;

namespace Elsa.ProtoActor;

public record BookmarksStored(ICollection<StoredBookmark> Bookmarks);
public record BookmarksRemovedByWorkflow(string WorkflowInstanceId);