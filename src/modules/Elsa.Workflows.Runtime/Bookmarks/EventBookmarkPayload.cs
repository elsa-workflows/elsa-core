using Elsa.Common;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Workflows.Runtime.Bookmarks;

[Obsolete("Use EventStimulus instead.")]
[ForwardedType(typeof(EventStimulus))]
public record EventBookmarkPayload(string EventName);