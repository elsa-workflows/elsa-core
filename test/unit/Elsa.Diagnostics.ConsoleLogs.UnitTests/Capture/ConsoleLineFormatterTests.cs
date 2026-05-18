using Elsa.Diagnostics.ConsoleLogs.Services;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests.Capture;

public class ConsoleLineFormatterTests
{
    [Fact]
    public void Format_StripsAnsiByDefault()
    {
        var formatter = new ConsoleLineFormatter(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions()));

        var result = formatter.Format("\u001b[31mred\u001b[0m");

        Assert.Equal("red", result.Text);
        Assert.False(result.Truncated);
    }

    [Fact]
    public void Format_PreservesAnsiWhenConfigured()
    {
        var formatter = new ConsoleLineFormatter(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions { StripAnsiEscapeSequences = false }));

        var result = formatter.Format("\u001b[31mred\u001b[0m");

        Assert.Equal("\u001b[31mred\u001b[0m", result.Text);
    }

    [Fact]
    public void Format_TruncatesOversizedLine()
    {
        var formatter = new ConsoleLineFormatter(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions { MaxLineLength = 3 }));

        var result = formatter.Format("abcdef");

        Assert.Equal("abc", result.Text);
        Assert.True(result.Truncated);
    }
}
