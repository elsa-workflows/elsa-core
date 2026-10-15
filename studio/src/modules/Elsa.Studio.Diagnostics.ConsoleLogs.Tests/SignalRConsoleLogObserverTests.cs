using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.ConsoleLogs.Models;
using Elsa.Studio.Diagnostics.ConsoleLogs.Services;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Tests;

public class SignalRConsoleLogObserverTests
{
    [Fact]
    public async Task StartAsync_PublishesErrorStatus_WhenConnectionFails()
    {
        var observer = new SignalRConsoleLogObserver(
            new TestBackendApiClientProvider(new("http://127.0.0.1:1/")),
            new NoopConnectionOptionsConfigurator(),
            NullLogger<SignalRConsoleLogObserver>.Instance);
        var statuses = new List<ConsoleLogConnectionStatus>();
        observer.ConnectionStatusChanged += status =>
        {
            statuses.Add(status);
            return Task.CompletedTask;
        };

        await observer.StartAsync(new());

        Assert.Contains(ConsoleLogConnectionStatus.Connecting, statuses);
        Assert.Contains(ConsoleLogConnectionStatus.Error, statuses);
        await observer.DisposeAsync();
    }

    [Fact]
    public async Task ReconnectAsync_PreservesLatestFilter_WhenConnectionFails()
    {
        var observer = new SignalRConsoleLogObserver(
            new TestBackendApiClientProvider(new("http://127.0.0.1:1/")),
            new NoopConnectionOptionsConfigurator(),
            NullLogger<SignalRConsoleLogObserver>.Instance);
        var filter = new ConsoleLogFilter { SourceId = "source-a", Query = "needle" };

        await observer.ReconnectAsync(filter);

        Assert.Equal("source-a", observer.CurrentFilter?.SourceId);
        Assert.Equal("needle", observer.CurrentFilter?.Query);
        await observer.DisposeAsync();
    }

    [Fact]
    public async Task UpdateFilterAsync_PreservesLatestFilter_WhenDisconnected()
    {
        var observer = new SignalRConsoleLogObserver(
            new TestBackendApiClientProvider(new("http://127.0.0.1:1/")),
            new NoopConnectionOptionsConfigurator(),
            NullLogger<SignalRConsoleLogObserver>.Instance);

        await observer.UpdateFilterAsync(new() { SourceId = "source-b", Query = "updated" });

        Assert.Equal("source-b", observer.CurrentFilter?.SourceId);
        Assert.Equal("updated", observer.CurrentFilter?.Query);
        await observer.DisposeAsync();
    }

    private class TestBackendApiClientProvider(Uri url) : IBackendApiClientProvider
    {
        public Uri Url { get; } = url;
        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();
    }

    private class NoopConnectionOptionsConfigurator : IHttpConnectionOptionsConfigurator
    {
        public Task ConfigureAsync(HttpConnectionOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
