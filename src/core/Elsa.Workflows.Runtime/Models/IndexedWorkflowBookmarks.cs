using Elsa.Persistence.Entities;
using Elsa.State;

namespace Elsa.Workflows.Runtime.Models;

public record IndexedWorkflowBookmarks(WorkflowState WorkflowState, IReadOnlyCollection<WorkflowBookmark> AddedBookmarks, IReadOnlyCollection<WorkflowBookmark> RemovedBookmarks);