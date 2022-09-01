using System.Collections.Generic;
using Elsa.Runtime.Protos;

namespace Elsa.ProtoActor;

public record WorkflowStarted(string DefinitionId, int Version, string? CorrelationId, IDictionary<string, object>? Input);
public record WorkflowResumed(string BookmarkId, IDictionary<string, object>? Input);
public record BookmarkStored(StoredBookmark Bookmark);
public record BookmarkRemoved(string BookmarkId);