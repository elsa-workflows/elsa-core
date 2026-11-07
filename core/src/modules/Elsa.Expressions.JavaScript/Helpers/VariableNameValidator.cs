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

    private static bool IsValidChar(char c)
    {
        return char.IsLetterOrDigit(c) || c is '_' or '$';
    }
}
