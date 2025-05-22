namespace Elsa.Expressions.CSharp.Extensions;

public static class VariableNameExtensions
{
    public static bool IsValidVariableName(this string name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.All(char.IsLetterOrDigit);
    }
    
    public static bool IsInvalidVariableName(this string name)
    {
        return !IsValidVariableName(name);
    }
    
    public static IEnumerable<string> FilterInvalidVariableNames(this IEnumerable<string> names)
    {
        return names.Where(IsValidVariableName);
    }
}