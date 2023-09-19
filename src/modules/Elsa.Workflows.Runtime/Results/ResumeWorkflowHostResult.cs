using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Results;

public record ResumeWorkflowHostResult(Diff<Bookmark> BookmarksDiff);