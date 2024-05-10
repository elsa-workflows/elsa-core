using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
// ReSharper disable ExplicitCallerInfoArgument

namespace Elsa.Workflows.Activities;

/// <summary>
/// Faults the workflow.
/// </summary>
[Activity("Elsa", "Primitives", "Faults the workflow.")]
[PublicAPI]
public class Fault : Activity
{
    /// <inheritdoc />
    public Fault([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }
    
    /// <summary>
    /// Creates a fault activity.
    /// </summary>
    public static Fault Create(string code, string category, string type, string? message = null, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null)
    {
        return new Fault(source, line)
        {
            Code = new(code),
            Message = new(message),
            Category = new(category),
            FaultType = new(type)
        };
    }

    /// <summary>
    /// Code to categorize the fault.
    /// </summary>
    [Input(Description = "Code to categorize the fault.")]
    public Input<string> Code { get; set; } = default!;
    
    /// <summary>
    /// Category to categorize the fault. Examples: HTTP, Alteration, Azure, etc.
    /// </summary>
    [Input(Description = "Category to categorize the fault. Examples: HTTP, Alteration, Azure, etc.")]
    public Input<string> Category { get; set; } = default!;
    
    /// <summary>
    /// The type of fault. Examples: System, Business, Integration, etc.
    /// </summary>
    [Input(
        DisplayName = "Type",
        Description = "The type of fault. Examples: System, Business, Integration, etc."
        )]
    public Input<string> FaultType { get; set; } = default!;

    /// <summary>
    /// The message to include with the fault.
    /// </summary>
    [Input(Description = "The message to include with the fault.")]
    public Input<string?> Message { get; set; } = default!;

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var code = Code.GetOrDefault(context) ?? "0";
        var category = Category.GetOrDefault(context) ?? "General";
        var type = FaultType.GetOrDefault(context) ?? "System";
        var message = Message.GetOrDefault(context);
        throw new FaultException(code, category, type, message);
    }
}