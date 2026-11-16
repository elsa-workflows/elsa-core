namespace Elsa.Expressions.JavaScript.Helpers;

public static class VariableNameValidator
{
    /// <summary>
    /// Returns true if the specified name consists of letters, digits, underscores or dollar signs.
    /// </summary>
    /// <remarks>
    /// A leading digit is deliberately allowed: such a name is not a valid identifier after <c>variables.</c>, but its
    /// generated accessors (e.g. <c>get1234()</c>) are, and existing workflows rely on them.
    /// </remarks>
    public static bool IsValidVariableName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.All(IsValidChar);
    }

    /// <summary>
    /// Returns true if the specified variable name consists of letters and digits only.
    /// </summary>
    [Obsolete("Accessors are no longer decided per name: a name containing '_' or '$' also gets them unless another variable already claims the accessor. Removed in the next major version.")]
    public static bool HasVariableAccessors(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.All(char.IsLetterOrDigit);
    }

    private static bool IsValidChar(char c)
    {
        return char.IsLetterOrDigit(c) || c is '_' or '$';
    }
}
