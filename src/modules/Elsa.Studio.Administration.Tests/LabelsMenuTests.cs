using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;
using Elsa.Studio.Contracts;
using Elsa.Studio.Labels;
using Elsa.Studio.Labels.Menu;
using Elsa.Studio.Localization;
using Elsa.Studio.WorkflowContexts.Widgets;
using Microsoft.Extensions.Localization;
using System.Net;
using Xunit;

namespace Elsa.Studio.Labels.Tests;

public class LabelsMenuTests
{
    [Fact]
    public async Task GetMenuItemsAsync_WhenRemoteFeatureDisabled_ReturnsNoItems()
    {
        var menu = CreateMenu(new TestRemoteFeatureProvider(false));

        var items = await menu.GetMenuItemsAsync();

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetMenuItemsAsync_WhenRemoteFeatureEnabled_ReturnsLabelsItem()
    {
        var provider = new TestRemoteFeatureProvider(true);
        var items = (await CreateMenu(provider).GetMenuItemsAsync()).ToList();

        var item = Assert.Single(items);
        Assert.Equal(Feature.RemoteFeatureName, provider.RequestedFeatureName);
        Assert.Equal("Labels", item.Href);
        Assert.Equal("Labels", item.Text);
    }

    [Fact]
    public async Task GetMenuItemsAsync_WhenRemoteFeatureCheckFailsAuthentication_ReturnsNoItems()
    {
        var menu = CreateMenu(new ThrowingRemoteFeatureProvider(new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized)));

        var items = await menu.GetMenuItemsAsync();

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetMenuItemsAsync_WhenRemoteFeatureCheckIsCanceled_ReturnsNoItems()
    {
        var menu = CreateMenu(new ThrowingRemoteFeatureProvider(new TaskCanceledException()));

        var items = await menu.GetMenuItemsAsync();

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetMenuItemsAsync_WhenRemoteFeatureCheckRunsDuringPrerender_ReturnsNoItems()
    {
        var menu = CreateMenu(new ThrowingRemoteFeatureProvider(new InvalidOperationException("JavaScript interop calls cannot be issued at this time.")));

        var items = await menu.GetMenuItemsAsync();

        Assert.Empty(items);
    }

    private static LabelsMenu CreateMenu(IRemoteFeatureProvider remoteFeatureProvider) =>
        new(new TestLocalizer(), remoteFeatureProvider);

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] =>
            new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }

    private sealed class TestRemoteFeatureProvider(bool enabled) : IRemoteFeatureProvider
    {
        public string? RequestedFeatureName { get; private set; }

        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
        {
            RequestedFeatureName = featureName;
            return Task.FromResult(enabled && featureName == Feature.RemoteFeatureName);
        }

        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }

    private sealed class ThrowingRemoteFeatureProvider(Exception exception) : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
            Task.FromException<bool>(exception);

        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }
}

public class LabelsFeatureTests
{
    [Fact]
    public void Feature_DeclaresCanonicalRemoteFeature()
    {
        var attribute = Assert.Single(typeof(Feature).GetCustomAttributes(typeof(RemoteFeatureAttribute), false).OfType<RemoteFeatureAttribute>());

        Assert.Equal("Elsa.Labels.ShellFeatures.Labels", Feature.RemoteFeatureName);
        Assert.Equal(Feature.RemoteFeatureName, attribute.Name);
    }

    [Fact]
    public async Task Feature_InitializeAsync_RegistersLabelsWidget()
    {
        var registry = new TestWidgetRegistry();

        await new Feature(registry).InitializeAsync();

        var widget = Assert.Single(registry.List("workflow-definition-properties"));
        Assert.IsType<WorkflowDefinitionLabelsEditorWidget>(widget);
    }

    private sealed class TestWidgetRegistry : IWidgetRegistry
    {
        private readonly List<IWidget> _widgets = [];

        public void Add(IWidget widget) => _widgets.Add(widget);

        public IEnumerable<IWidget> List(string zone) => _widgets.Where(widget => widget.Zone == zone);
    }
}
