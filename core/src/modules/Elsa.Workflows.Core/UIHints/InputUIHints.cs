// ReSharper disable once CheckNamespace
namespace Elsa.Workflows.UIHints;

/// <summary>
/// Provides a set of well-known UI hints.
/// </summary>
public static class InputUIHints
{
    public const string Checkbox = "checkbox";
    public const string CheckList = "checklist";
    public const string CodeEditor = "code-editor";
    public const string Dictionary = "dictionary";
    public const string DateTimePicker = "datetime-picker";
    public const string DropDown = "dropdown";
    public const string DynamicOutcomes = "dynamic-outcomes";
    public const string ExpressionEditor = "expression-editor";
    public const string JsonEditor = "json-editor";
    public const string MultiLine = "multiline";
    public const string MultiText = "multitext";
    public const string OutputPicker = "output-picker";
    public const string OutcomePicker = "outcome-picker";
    public const string RadioList = "radiolist";
    public const string SingleLine = "singleline";

    /// <summary>
    /// Displays a single-line input that masks the literal value on screen.
    /// Masking is presentational: a literal is stored in clear text in the workflow definition.
    /// Read a real secret from configuration or a secret store with an expression instead of a literal.
    /// </summary>
    public const string Password = "password";

    public const string TypePicker = "type-picker";
    public const string VariablePicker = "variable-picker";
    public const string InputPicker = "input-picker";
    public const string WorkflowDefinitionPicker = "workflow-definition-picker";
}