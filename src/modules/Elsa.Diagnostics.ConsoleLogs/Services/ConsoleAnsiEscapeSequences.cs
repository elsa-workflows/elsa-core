using System.Text.RegularExpressions;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

internal static class ConsoleAnsiEscapeSequences
{
    private static readonly Regex AnsiRegex = new(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Strip(string text) => AnsiRegex.Replace(text, string.Empty);
}
