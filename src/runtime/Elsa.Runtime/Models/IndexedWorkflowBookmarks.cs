using Elsa.Persistence.Entities;
using Elsa.State;

namespace Elsa.Runtime.Models;

public record IndexedWorkflowBookmarks(WorkflowState WorkflowState, IReadOnlyCollection<WorkflowBookmark> AddedBookmarks, IReadOnlyCollection<WorkflowBookmark> RemovedBookmarks);