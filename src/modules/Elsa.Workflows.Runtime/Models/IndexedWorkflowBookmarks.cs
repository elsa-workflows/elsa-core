using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Models;

public record IndexedWorkflowBookmarks(WorkflowState WorkflowState, ICollection<Bookmark> AddedBookmarks, ICollection<Bookmark> RemovedBookmarks);