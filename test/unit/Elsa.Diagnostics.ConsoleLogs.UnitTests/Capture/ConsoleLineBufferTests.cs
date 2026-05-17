using Elsa.Diagnostics.ConsoleLogs.Services;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests.Capture;

public class ConsoleLineBufferTests
{
    private readonly ConsoleLineBuffer _buffer = new(Microsoft.Extensions.Options.Options.Create(new ConsoleLogsOptions { MaxLineLength = 5, IdleFlushTimeout = TimeSpan.FromSeconds(1) }));

    [Fact]
    public void Append_BuffersPartialWritesUntilNewline()
    {
        Assert.Empty(_buffer.Append("hel", DateTimeOffset.UtcNow));

        var lines = _buffer.Append("lo\n", DateTimeOffset.UtcNow);

        Assert.Equal(["hello"], lines);
    }

    [Fact]
    public void Append_CompletesLineAtMaximumLength()
    {
        var lines = _buffer.Append("hello!", DateTimeOffset.UtcNow);

        Assert.Equal(["hello"], lines);
        Assert.Equal("!", _buffer.Flush());
    }

    [Fact]
    public void FlushIfIdle_CompletesBufferedLineAfterTimeout()
    {
        var now = DateTimeOffset.UtcNow;

        _buffer.Append("tail", now);

        Assert.Null(_buffer.FlushIfIdle(now.AddMilliseconds(500)));
        Assert.Equal("tail", _buffer.FlushIfIdle(now.AddSeconds(2)));
    }
}
