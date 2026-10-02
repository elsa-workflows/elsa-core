using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.AI.Menu;
using Elsa.Studio.Contracts;
using System.Net;
using Xunit;

namespace Elsa.Studio.AI.Tests;

public class WeaverMenuTests
{
    [Fact]
    public async Task GetMenuItemsAsync_WhenRemoteFeatureDisabled_ReturnsNoItems()
    {
        var menu = new WeaverMenu(new TestRemoteFeatureProvider(false));

        var items = await menu.GetMenuItemsAsync();

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetMenuItemsAsync_WhenRemoteFeatureEnabled_ReturnsWeaverItem()
    {
        var menu = new WeaverMenu(new TestRemoteFeatureProvider(true));

        var items = (await menu.GetMenuItemsAsync()).ToList();

        var item = Assert.Single(items);
        Assert.Equal("ai/weaver", item.Href);
        Assert.Equal("Weaver", item.Text);
    }

    [Fact]
    public async Task GetMenuItemsAsync_WhenRemoteFeatureCheckFails_ReturnsNoItems()
    {
        var menu = new WeaverMenu(new ThrowingRemoteFeatureProvider(new HttpRequestException("Forbidden", null, HttpStatusCode.Forbidden)));

        var items = await menu.GetMenuItemsAsync();

        Assert.Empty(items);
    }

    private class TestRemoteFeatureProvider(bool enabled) : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
            Task.FromResult(enabled && featureName == Feature.RemoteFeatureName);

        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }

    private class ThrowingRemoteFeatureProvider(Exception exception) : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => throw exception;

        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }
}
