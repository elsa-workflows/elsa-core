using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Attributes;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Mark the workflow as finished.
/// </summary>
[Activity("Elsa", "Primitives", "A milestone activity with no behavior other than marking a milestone.", Kind = ActivityKind.Action)]
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