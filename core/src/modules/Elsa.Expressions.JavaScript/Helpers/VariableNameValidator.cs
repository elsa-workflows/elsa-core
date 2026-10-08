namespace Elsa.Expressions.JavaScript.Helpers;

public static class VariableNameValidator
{
    public static bool IsValidVariableName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.All(char.IsLetterOrDigit);
    }
}