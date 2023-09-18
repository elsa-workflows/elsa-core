using System.Runtime.CompilerServices;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Marks the end of a flowchart, causing the flowchart to complete.
/// </summary>
[Activity("Elsa", "Flow", "A milestone activity that marks the end of a flowchart.", Kind = ActivityKind.Action)]
[PublicAPI]
public class End : CodeActivity, ITerminalNode
{
    /// <inheritdoc />
    public End([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
}