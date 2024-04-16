using Elsa.Workflows.Attributes;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.Runtime.Stimuli;

/// <summary>
/// Contains information created by <see cref="RunTask"/>.  
/// </summary>
public record RunTaskStimulus(string TaskId, [property: ExcludeFromHash]string TaskName);