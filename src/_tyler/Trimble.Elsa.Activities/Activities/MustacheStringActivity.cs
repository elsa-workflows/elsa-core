using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using System.Text.Json.Serialization;

namespace Trimble.Elsa.Activities.Activities;

/// <summary>
/// Replaces mustache variables in the template property with the values from
/// the corresponding variable names.
/// </summary>
[Activity(
    "Trimble.Elsa.Activities.Activities",
    "ServiceRegistry",
    "Transforms strings using mustache syntax.",
    DisplayName = "Mustache Transform",
    Kind = ActivityKind.Task)]
public class MustacheStringActivity : CodeActivity<string>
{
    /// <summary>
    /// Do not use for construction. Exists only to support serialization.
    /// </summary>
    [JsonConstructor]
    private MustacheStringActivity() {}

    /// <summary>
    /// Primary constructor.
    /// </summary>
    public MustacheStringActivity(string template, Variable<string> resultVariable)
    {
        Template = new(template);
        Result = new(resultVariable);
    }

    /// <summary>
    /// A mustache syntax string containing variable names.
    /// E.g. "Bearer {{myTokenVariableName}}"
    /// </summary>
    public Input<string> Template { get; set; } = default!;

    /// <summary>
    /// Ensures that the value set by the activity is encrypted if it refers to
    /// an EncryptedVariable.
    /// </summary>
    protected override void Execute(ActivityExecutionContext context)
    {
        if (Result is null)
        {
            context.LogInfo<MustacheStringActivity>(
                $"Could not execute {nameof(MustacheStringActivity)} -- value of Result property is null");
            return;
        }

        string? decrypted = Template.MemoryBlockReference() is EncryptedVariableString encrypted
            ? encrypted.Decrypt(context)
            : Template.Get(context);

        var replaced = decrypted?.ReplaceTokens(context);

        // All activities with output should call SetOutputAndCheckEncryption.
        // This may require implementing a new activity type to override default 
        // behavior.
        context.SetOutputAndCheckEncryption(Result, replaced);
    }
}
