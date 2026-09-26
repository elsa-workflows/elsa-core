using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Elsa.Studio.Diagnostics.ConsoleLogs.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Tests;

public class ConsoleLogUrlStateMapperTests
{
    private readonly ConsoleLogUrlStateMapper _mapper = new();

    [Fact]
    public void ApplyQuery_ParsesCanonicalParameters()
    {
        var state = new ConsoleLogViewState();
        var uri = new Uri("https://studio/diagnostics/console?source=source-a&stream=stderr&text=failed&from=2026-05-26T10:00:00.0000000%2B00:00&to=2026-05-26T10:05:00.0000000%2B00:00&wrap=true&compact=false&ansi=false&follow=false");

        _mapper.ApplyQuery(state, uri);

        Assert.Equal("source-a", state.Filter.SourceId);
        Assert.Equal(ConsoleLogStream.Stderr, state.Filter.Stream);
        Assert.Equal("failed", state.Filter.Query);
        Assert.Equal(new DateTimeOffset(2026, 5, 26, 10, 0, 0, TimeSpan.Zero), state.Filter.From);
        Assert.Equal(new DateTimeOffset(2026, 5, 26, 10, 5, 0, TimeSpan.Zero), state.Filter.To);
        Assert.True(state.Wrap);
        Assert.False(state.Compact);
        Assert.False(state.Ansi);
        Assert.False(state.FollowTail);
    }

    [Fact]
    public void ApplyQuery_InvalidStreamDefaultsToBoth()
    {
        var state = new ConsoleLogViewState();

        _mapper.ApplyQuery(state, new("https://studio/diagnostics/console?stream=nope"));

        Assert.Null(state.Filter.Stream);
    }

    [Fact]
    public void ToQueryParameters_UsesCanonicalNames()
    {
        var state = new ConsoleLogViewState { Wrap = true, Compact = false, Ansi = false, FollowTail = true };
        state.Filter.SourceId = "source-a";
        state.Filter.Stream = ConsoleLogStream.Stdout;
        state.Filter.Query = "failed";
        state.Filter.From = new DateTimeOffset(2026, 5, 26, 10, 0, 0, TimeSpan.Zero);
        state.Filter.To = new DateTimeOffset(2026, 5, 26, 10, 5, 0, TimeSpan.Zero);

        var parameters = _mapper.ToQueryParameters(state);

        Assert.Equal("source-a", parameters["source"]);
        Assert.Equal("stdout", parameters["stream"]);
        Assert.Equal("failed", parameters["text"]);
        Assert.Equal("2026-05-26T10:00:00.0000000+00:00", parameters["from"]);
        Assert.Equal("2026-05-26T10:05:00.0000000+00:00", parameters["to"]);
        Assert.Equal("true", parameters["wrap"]);
        Assert.Equal("false", parameters["compact"]);
        Assert.Equal("false", parameters["ansi"]);
        Assert.Equal("true", parameters["follow"]);
    }
}
