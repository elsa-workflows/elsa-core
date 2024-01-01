using Elsa.Workflows.State;

namespace Elsa.Workflows.Models;

public record InvokeWorkflowResult(WorkflowState WorkflowState, ICollection<Bookmark> Bookmarks);