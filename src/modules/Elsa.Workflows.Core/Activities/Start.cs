using System.Runtime.CompilerServices;
using Elsa.Workflows.Core.Attributes;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Marks the start of a flowchart.
/// </summary>
[Activity("Elsa", "Flow", "A milestone activity that marks the start of a flowchart.", Kind = ActivityKind.Action)]
[PublicAPI]
public class Start : CodeActivity
{
    /// <inheritdoc />
    public Start([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
}