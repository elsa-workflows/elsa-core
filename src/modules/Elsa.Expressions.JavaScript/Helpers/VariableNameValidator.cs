namespace Elsa.Expressions.JavaScript.Helpers;

public static class VariableNameValidator
{
    /// <summary>
    /// Returns true if the specified name can be used as a JavaScript identifier, which is the case when it starts with
    /// a letter, an underscore or a dollar sign, followed by letters, digits, underscores or dollar signs.
    /// </summary>
    public static bool IsValidVariableName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (!IsValidFirstChar(name[0]))
            return false;

        for (var i = 1; i < name.Length; i++)
        {
            if (!IsValidChar(name[i]))
                return false;
        }

        return true;
    }

    private static bool IsValidFirstChar(char c)
    {
        return char.IsLetter(c) || c is '_' or '$';
    }

    private static bool IsValidChar(char c)
    {
        return char.IsLetterOrDigit(c) || c is '_' or '$';
    }
}
