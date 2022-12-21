using Elsa.ProtoActor.Protos;

namespace Elsa.ProtoActor;

internal record BookmarksStored(ICollection<StoredBookmark> Bookmarks);

internal record BookmarksRemovedByWorkflow(string WorkflowInstanceId);