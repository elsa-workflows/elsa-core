using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Models;

public record RunWorkflowResult(WorkflowState WorkflowState, ICollection<Bookmark> Bookmarks);