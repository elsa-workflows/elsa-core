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
    /// Returns true if a <c>get{Name}</c>/<c>set{Name}</c> accessor pair should be generated for the specified variable name.
    /// </summary>
    /// <remarks>
    /// Accessor names are built with <c>Pascalize()</c>, which drops underscores, so <c>foo_bar</c> and <c>fooBar</c> would both
    /// map to <c>getFooBar</c>. Only names made of letters and digits get accessors, which keeps every accessor pointing at the
    /// same variable it did before <c>_</c> and <c>$</c> were allowed. Other valid names are reached through
    /// <c>variables.&lt;name&gt;</c> or <c>getVariable("&lt;name&gt;")</c>.
    /// </remarks>
    public static bool HasVariableAccessors(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.All(char.IsLetterOrDigit);
    }

    private static bool IsValidChar(char c)
    {
        return char.IsLetterOrDigit(c) || c is '_' or '$';
    }
}
