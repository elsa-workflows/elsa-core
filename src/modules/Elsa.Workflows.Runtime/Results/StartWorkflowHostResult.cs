using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Results;

public record ExecuteWorkflowResult(Diff<Bookmark> BookmarksDiff);