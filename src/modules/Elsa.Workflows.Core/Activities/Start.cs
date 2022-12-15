using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Mark the workflow as finished.
/// </summary>
[Activity("Elsa", "Control Flow", "A milestone activity with no behavior other than marking a milestone.", Kind = ActivityKind.Action)]
public class Start : Activity
{
    /// <inheritdoc />
    [JsonConstructor]
    public Start([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
}