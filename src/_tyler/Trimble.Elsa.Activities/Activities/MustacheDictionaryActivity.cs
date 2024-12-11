using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using System.Text.Json.Serialization;

namespace Trimble.Elsa.Activities.Activities;

/// <summary>
/// Replaces mustache variables in the Template property with the values from
/// the corresponding variable names.
/// </summary>
[Activity(
    "Trimble.Elsa.Activities.Activities",
    "ServiceRegistry",
    "Transforms strings in a dictionary using mustache syntax.",
    DisplayName = "Mustache Dictionary Transform",
    Kind = ActivityKind.Task)]
public class MustacheDictionaryActivity : CodeActivity<Dictionary<string, string>>
{
    /// <summary>
    /// Parameterless constructor used primarily in JSON serdes operations.
    /// </summary>
    [JsonConstructor]
    public MustacheDictionaryActivity() {}

    /// <summary>
    /// Primary constructor.
    /// </summary>
    public MustacheDictionaryActivity(Dictionary<string, string> template)
        => Template = new(template);

    /// <summary>
    /// A mustache-syntax string containing variable names.
    /// E.g. "Bearer {{myTokenVariableName}}"
    /// </summary>
    public Input<Dictionary<string, string>> Template { get; set; } = default!;

    /// <summary>
    /// Ensures that the value set by the activity is encrypted if it refers to
    /// an EncryptedVariable.
    /// </summary>
    protected override void Execute(ActivityExecutionContext context)
    {
        Dictionary<string, string> decryptedTemplateValues =
            (Template.MemoryBlockReference() is EncryptedVariableDictionary dict)
            ? dict.Decrypt(context)
            : Template.Get(context);

        var replaced = decryptedTemplateValues.ReplaceTokens(context);

        // All activities with output should call SetOutputAndCheckEncryption
        // This may require implementing a new activity type to override default
        // behavior.
        if (Result is null)
        {
            return;
        }
        context.SetOutputAndCheckEncryption(Result, replaced);
    }
}
