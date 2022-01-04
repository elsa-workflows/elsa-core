using System.Collections;
using Elsa.Attributes;
using Elsa.Management.Extensions;

namespace Elsa.Management.Models;

public static class InputUIHints
{
    public const string SingleLine = "single-line";
    public const string MultiLine = "multi-line";
    public const string Checkbox = "checkbox";
    public const string CheckList = "check-list";
    public const string RadioList = "radio-list";
    public const string Dropdown = "dropdown";
    public const string MultiText = "multi-text";
    public const string CodeEditor = "code-editor";

    /// <summary>
    /// An editor that allows the user to write a blob of JSON.
    /// </summary>
    public const string Json = "json";

    public static string GetUIHint(Type wrappedPropertyType, InputAttribute? inputAttribute)
    {
        if (inputAttribute?.UIHint != null)
            return inputAttribute.UIHint;

        if (wrappedPropertyType == typeof(bool) || wrappedPropertyType == typeof(bool?))
            return InputUIHints.Checkbox;

        if (wrappedPropertyType == typeof(string))
            return InputUIHints.SingleLine;

        if (typeof(IEnumerable).IsAssignableFrom(wrappedPropertyType))
            return InputUIHints.Dropdown;

        if (wrappedPropertyType.IsEnum || wrappedPropertyType.IsNullableType() && wrappedPropertyType.GetTypeOfNullable().IsEnum)
            return InputUIHints.Dropdown;

        return InputUIHints.SingleLine;
    }
}