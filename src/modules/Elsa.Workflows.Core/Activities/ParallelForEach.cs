using System.Runtime.CompilerServices;
using Elsa.Workflows.Core.Attributes;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Schedule an activity for each item in parallel.
/// </summary>
[Activity("Elsa", "Looping", "Schedule an activity for each item in parallel.")]
public class ParallelForEach : ParallelForEach<object>
{
    /// <inheritdoc />
    public ParallelForEach([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
}