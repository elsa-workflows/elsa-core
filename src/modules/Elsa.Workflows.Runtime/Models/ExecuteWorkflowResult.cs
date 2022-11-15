using System.Collections.Generic;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Models;

public record ExecuteWorkflowResult(WorkflowState WorkflowState, ICollection<Bookmark> Bookmarks);