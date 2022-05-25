using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Models;

public record InvokeWorkflowResult(WorkflowState WorkflowState, IReadOnlyCollection<Bookmark> Bookmarks);