using Elsa.Workflows.Core.State;
using Elsa.Workflows.Persistence.Entities;

namespace Elsa.Workflows.Runtime.Models;

public record IndexedWorkflowBookmarks(WorkflowState WorkflowState, IReadOnlyCollection<WorkflowBookmark> AddedBookmarks, IReadOnlyCollection<WorkflowBookmark> RemovedBookmarks);