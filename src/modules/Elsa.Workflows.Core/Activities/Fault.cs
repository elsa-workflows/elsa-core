using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Exceptions;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Faults the workflow.
/// </summary>
[Activity("Elsa", "Primitives", "Faults the workflow.")]
[PublicAPI]
public class Fault : Activity
{
    /// <inheritdoc />
    public Fault([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The message to include with the fault.
    /// </summary>
    [Input(Description = "The message to include with the fault.")]
    public Input<string> Message { get; set; } = default!;

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var message = Message.GetOrDefault(context);
        throw new FaultException(message);
    }
}