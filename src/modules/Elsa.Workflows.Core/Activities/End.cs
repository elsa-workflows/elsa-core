using System.Runtime.CompilerServices;
using Elsa.Workflows.Core.Attributes;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Marks the end of a flowchart, causing the flowchart to complete.
/// </summary>
[Activity("Elsa", "Flow", "A milestone activity that marks the start of a flowchart.", Kind = ActivityKind.Action)]
[PublicAPI]
public class End : CodeActivity
{
    /// <inheritdoc />
    public End([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
}