using Elsa.State;

namespace Elsa.Models;

public record InvokeWorkflowResult(WorkflowState WorkflowState, IReadOnlyCollection<Bookmark> Bookmarks);