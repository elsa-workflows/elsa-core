using Elsa.Common;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Workflows.Runtime.Bookmarks;

/// <summary>
/// Contains information created by <see cref="RunTask"/>.  
/// </summary>
[Obsolete("Use RunTaskStimulus instead.")]
[ForwardedType(typeof(RunTaskStimulus))]
public record RunTaskBookmarkPayload(string TaskId, [property: ExcludeFromHash] string TaskName);