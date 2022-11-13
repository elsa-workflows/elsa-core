using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Models;

public record InvokeWorkflowResult(WorkflowState WorkflowState, ICollection<Bookmark> Bookmarks);