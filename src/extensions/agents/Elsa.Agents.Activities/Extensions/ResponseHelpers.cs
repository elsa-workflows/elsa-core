namespace Elsa.Agents.Activities.Extensions;

public static class ResponseHelpers
{
    public static bool IsJsonResponse(string text)
    {
        return text.StartsWith("{", StringComparison.OrdinalIgnoreCase) || text.StartsWith("[", StringComparison.OrdinalIgnoreCase);
    }

    public static string StripCodeFences(string content)
    {
        var trimmed = content.Trim();

        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            return trimmed;

        var lines = trimmed.Split('\n');
        return lines.Length < 2 ? trimmed : string.Join('\n', lines.Skip(1).Take(lines.Length - 2)).Trim();
    }
}