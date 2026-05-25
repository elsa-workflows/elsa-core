using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public class ConsoleLineFormatter(IOptions<ConsoleLogsOptions> options)
{
    private readonly ConsoleLogsOptions _options = options.Value;

    public FormattedConsoleLine Format(string text)
    {
        if (_options.StripAnsiEscapeSequences)
            text = ConsoleAnsiEscapeSequences.Strip(text);

        var truncated = text.Length > _options.MaxLineLength;
        if (truncated)
            text = text[.._options.MaxLineLength];

        return new(text, truncated);
    }
}

public record FormattedConsoleLine(string Text, bool Truncated);
