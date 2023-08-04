using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Attributes;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Marks the start of a flowchart.
/// </summary>
[Activity("Elsa", "Flow", "A milestone activity with no behavior other than marking the start of a flowchart.", Kind = ActivityKind.Action)]
[PublicAPI]
public class Start : CodeActivity
{
    /// <inheritdoc />
    [JsonConstructor]
    public Start()
    {
    }
    
    /// <inheritdoc />
    public Start([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
}