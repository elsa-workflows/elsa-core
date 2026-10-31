using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.OpenTelemetry.Menu;
using System.Net;
using Xunit;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Tests;

public class OpenTelemetryMenuTests
{
    [Fact]
    public async Task GetMenuItemsAsync_WhenRemoteFeatureDisabled_ReturnsNoItems()
    {
        var menu = new OpenTelemetryMenu(new TestRemoteFeatureProvider(false));

        var items = await menu.GetMenuItemsAsync();

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetMenuItemsAsync_WhenRemoteFeatureEnabled_ReturnsOpenTelemetryItem()
    {
        var menu = new OpenTelemetryMenu(new TestRemoteFeatureProvider(true));

        var items = (await menu.GetMenuItemsAsync()).ToList();

        var item = Assert.Single(items);
        Assert.Equal("diagnostics/opentelemetry", item.Href);
        Assert.Equal("OpenTelemetry", item.Text);
    }

    [Fact]
    public async Task GetMenuItemsAsync_WhenRemoteFeatureCheckFails_ReturnsNoItems()
    {
        var menu = new OpenTelemetryMenu(new ThrowingRemoteFeatureProvider(new HttpRequestException("Forbidden", null, HttpStatusCode.Forbidden)));

        var items = await menu.GetMenuItemsAsync();

        Assert.Empty(items);
    }

    private class TestRemoteFeatureProvider(bool enabled) : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(enabled && featureName == Feature.RemoteFeatureName);
        }

        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
        }
    }

    private class ThrowingRemoteFeatureProvider(Exception exception) : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
        {
            throw exception;
        }

        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
        }
    }
}
