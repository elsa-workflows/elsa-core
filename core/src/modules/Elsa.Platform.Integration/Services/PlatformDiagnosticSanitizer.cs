using System.Text.RegularExpressions;
using Elsa.Platform.Integration.Models;

namespace Elsa.Platform.Integration.Services;

public static partial class PlatformDiagnosticSanitizer
{
    public static PlatformDiagnostic Info(string code, string message) =>
        new(code, PlatformDiagnosticSeverity.Info, SafeMessage(message));

    public static PlatformDiagnostic Warning(string code, string message) =>
        new(code, PlatformDiagnosticSeverity.Warning, SafeMessage(message));

    public static PlatformDiagnostic Error(string code, string message) =>
        new(code, PlatformDiagnosticSeverity.Error, SafeMessage(message));

    public static string SafeMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "No diagnostic details were provided.";

        var safe = SensitiveWordsRegex().Replace(message, "[redacted]");
        return safe.Length <= 500 ? safe : safe[..500];
    }

    [GeneratedRegex("(api[-_ ]?key|bearer|client[-_ ]?secret|password|private[-_ ]?key|secret|token)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveWordsRegex();
}
