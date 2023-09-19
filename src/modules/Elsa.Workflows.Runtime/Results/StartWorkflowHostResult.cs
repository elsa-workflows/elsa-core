using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Results;

public record StartWorkflowHostResult(Diff<Bookmark> BookmarksDiff);