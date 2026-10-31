using System.Net;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Authorization;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Microsoft.Extensions.Localization;

namespace Elsa.Studio.Agents.Tests;

public class AgentsMenuTests
{
    [Fact]
    public async Task GetMenuItemsAsync_WhenRemoteFeatureDisabled_ReturnsNoItems()
    {
        var menu = CreateMenu(new StubRemoteFeatureProvider(false));

        Assert.Empty(await menu.GetMenuItemsAsync());
    }

    [Fact]
    public async Task GetMenuItemsAsync_WhenRemoteFeatureEnabled_ReturnsPermissionGatedAgentsItem()
    {
        var menu = CreateMenu(new StubRemoteFeatureProvider(true));

        var item = Assert.Single(await menu.GetMenuItemsAsync());

        Assert.Equal("ai/agents", item.Href);
        Assert.Equal("Agents", item.Text);
        Assert.Equal(new Permission("ai/agents", "read"), Assert.Single(item.RequiredPermissions));
    }

    [Fact]
    public async Task GetMenuItemsAsync_ChecksExactFeatureAndForwardsCancellationToken()
    {
        using var source = new CancellationTokenSource();
        var provider = new StubRemoteFeatureProvider(true);
        var menu = CreateMenu(provider);

        Assert.Single(await menu.GetMenuItemsAsync(source.Token));

        Assert.Equal(Feature.RemoteFeatureName, provider.RequestedFeatureName);
        Assert.Equal(source.Token, provider.RequestedCancellationToken);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task GetMenuItemsAsync_WhenFeatureDiscoveryFails_ReturnsNoItems(HttpStatusCode status)
    {
        var provider = new StubRemoteFeatureProvider(false, new HttpRequestException("Remote feature unavailable", null, status));

        Assert.Empty(await CreateMenu(provider).GetMenuItemsAsync());
    }

    [Fact]
    public async Task GetMenuItemsAsync_WhenFeatureDiscoveryIsCanceled_ReturnsNoItems()
    {
        var provider = new StubRemoteFeatureProvider(false, new TaskCanceledException());

        Assert.Empty(await CreateMenu(provider).GetMenuItemsAsync());
    }

    [Fact]
    public async Task GetMenuItemsAsync_WhenCalledDuringPrerender_ReturnsNoItems()
    {
        var provider = new StubRemoteFeatureProvider(false, new InvalidOperationException("JavaScript interop calls cannot be issued at this time."));

        Assert.Empty(await CreateMenu(provider).GetMenuItemsAsync());
    }

    private static AgentsMenu CreateMenu(IRemoteFeatureProvider featureProvider) =>
        new(new StubLocalizer(), featureProvider);

    private sealed class StubRemoteFeatureProvider(bool enabled, Exception? error = null) : IRemoteFeatureProvider
    {
        public string? RequestedFeatureName { get; private set; }
        public CancellationToken RequestedCancellationToken { get; private set; }

        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
        {
            RequestedFeatureName = featureName;
            RequestedCancellationToken = cancellationToken;
            return error is null
                ? Task.FromResult(enabled && featureName == Feature.RemoteFeatureName)
                : Task.FromException<bool>(error);
        }

        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }

    private sealed class StubLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);

        public LocalizedString this[string? key, params object[] arguments] =>
            new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
