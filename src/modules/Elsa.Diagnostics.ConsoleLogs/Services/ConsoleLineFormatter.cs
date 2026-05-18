using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public class ConsoleLineFormatter(IOptions<ConsoleLogsOptions> options)
{
    private static readonly Regex AnsiRegex = new(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly ConsoleLogsOptions _options = options.Value;

    public FormattedConsoleLine Format(string text)
    {
        if (_options.StripAnsiEscapeSequences)
            text = AnsiRegex.Replace(text, string.Empty);

        var truncated = text.Length > _options.MaxLineLength;
        if (truncated)
            text = text[.._options.MaxLineLength];

        return new(text, truncated);
    }
}

public record FormattedConsoleLine(string Text, bool Truncated);
