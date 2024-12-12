using Elsa.JavaScript.Helpers;

namespace Elsa.JavaScript.Extensions;

public static class VariableNameExtensions
{
    public static bool IsValidVariableName(this string name)
    {
        return VariableNameValidator.IsValidVariableName(name);
    }
    
    public static bool IsInvalidVariableName(this string name)
    {
        return !VariableNameValidator.IsValidVariableName(name);
    }
    
    public static IEnumerable<string> FilterInvalidVariableNames(this IEnumerable<string> names)
    {
        return names.Where(IsValidVariableName);
    }
}