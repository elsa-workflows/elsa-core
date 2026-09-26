using Bunit;
using Elsa.Studio.Components;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Localization.Time.Components;
using Elsa.Studio.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class DataPanelTimestampTests : BunitContext, IAsyncLifetime
{
    private readonly ClipboardStub _clipboard = new();

    public DataPanelTimestampTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IClipboard>(_clipboard);
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    public static IEnumerable<object[]> TimestampValues()
    {
        var instant = new DateTimeOffset(2025, 9, 2, 21, 36, 0, TimeSpan.Zero);
        foreach (var offsetHours in new[] { 0, -4 })
        foreach (var value in new object[] { instant, instant.ToOffset(TimeSpan.FromHours(2)), instant.UtcDateTime, instant.LocalDateTime, instant.DateTime })
            yield return [value, offsetHours];
    }

    [Theory]
    [MemberData(nameof(TimestampValues))]
    public async Task TimestampDisplayAndCopyUseTheConfiguredTimeZone(object value, int offsetHours)
    {
        var timeZone = TimeZoneInfo.CreateCustomTimeZone("Test zone", TimeSpan.FromHours(offsetHours), "Test zone", "Test zone");
        Services.AddSingleton<ITimeFormatter>(new DefaultTimeFormatter(new TimeZoneProviderStub(timeZone)));
        Render<MudPopoverProvider>();
        var item = new DataPanelItem(Label: "Created", Value: value, Format: DataPanelItemFormat.Timestamp, FormatString: "yyyy-MM-dd HH:mm:ss zzz");
        var panel = Render<DataPanel>(parameters => parameters.Add(x => x.Data, new DataPanelModel { item }));
        var expected = offsetHours == 0 ? "2025-09-02 21:36:00 +00:00" : "2025-09-02 17:36:00 -04:00";

        Assert.Equal(expected, panel.FindComponent<Timestamp>().Markup);
        Assert.Equal(expected, panel.Instance.GetFormattedValue(item));

        await panel.FindAll("button").Last().ClickAsync(new());
        Assert.Equal(expected, _clipboard.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void DefaultAndAutoFormatsMatchTheTimestampComponent(string? format)
    {
        Services.AddSingleton<ITimeFormatter>(new DefaultTimeFormatter(new TimeZoneProviderStub(TimeZoneInfo.Utc)));
        Render<MudPopoverProvider>();
        var value = new DateTimeOffset(2025, 9, 2, 21, 36, 0, TimeSpan.Zero);
        var item = new DataPanelItem(Label: "Updated", Value: value, Format: DataPanelItemFormat.Timestamp, FormatString: format);
        var panel = Render<DataPanel>(parameters => parameters.Add(x => x.Data, new DataPanelModel { item }));
        var timestamp = Render<Timestamp>(parameters => parameters.Add(x => x.Value, value));

        Assert.Equal(timestamp.Markup, panel.FindComponent<Timestamp>().Markup);
        Assert.Equal(timestamp.Markup, panel.Instance.GetFormattedValue(item));
        Assert.Equal(timestamp.Markup, panel.Instance.GetFormattedValue(item with { Format = DataPanelItemFormat.Auto }));
        Assert.Equal(string.Empty, panel.Instance.GetFormattedValue(item with { Value = null }));
    }

    private sealed class TimeZoneProviderStub(TimeZoneInfo timeZone) : ITimeZoneProvider
    {
        public TimeZoneInfo GetTimeZone() => timeZone;
    }

    private sealed class ClipboardStub : IClipboard
    {
        public string? Text { get; private set; }
        public Task CopyText(string text, CancellationToken cancellationToken = default)
        {
            Text = text;
            return Task.CompletedTask;
        }
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
