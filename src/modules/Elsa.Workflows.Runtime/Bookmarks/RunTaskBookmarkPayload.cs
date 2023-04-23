using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.Runtime.Bookmarks;

/// <summary>
/// Contains information created by <see cref="RunTask"/>.  
/// </summary>
public record RunTaskBookmarkPayload(string TaskId, [property: ExcludeFromHash]string TaskName);