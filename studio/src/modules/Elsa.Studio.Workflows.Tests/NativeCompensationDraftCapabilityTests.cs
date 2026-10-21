using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class NativeCompensationDraftCapabilityTests
{
    [Fact]
    public async Task IsEnabled_RequiresFreshCatalogCheck()
    {
        var provider = new FreshFeatureProvider(true);

        Assert.True(await NativeCompensationDraftCapability.IsEnabledAsync(provider));
        Assert.Equal(NativeCompensationDraftCapability.RemoteFeatureName, provider.LastFeatureName);
        Assert.Equal(1, provider.FreshChecks);
    }

    [Fact]
    public async Task IsEnabled_FailsClosedWhenProviderCannotRefreshItsCatalog()
    {
        var provider = new CachedOnlyFeatureProvider();

        Assert.False(await NativeCompensationDraftCapability.IsEnabledAsync(provider));
        Assert.Equal(0, provider.CachedChecks);
    }

    [Fact]
    public async Task SaveNativeDraft_RejectsSameUrlContextSwitchEvenWhenNewContextAdvertisesFeature()
    {
        const string backendUrl = "https://backend.example/";
        var backend = new ContextBackendApiClientProvider(new Uri(backendUrl), "environment-B|" + backendUrl);
        var mediator = DispatchProxy.Create<IMediator, CompletedMediator>();
        var featureProvider = new FreshFeatureProvider(true);
        var service = new WorkflowDefinitionEditorService(backend, mediator, featureProvider);
        var definition = new WorkflowDefinition
        {
            Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}""")!.AsObject()
        };

        var result = await service.SaveNativeCompensationDraftAsync(definition, false, "environment-A|" + backendUrl);

        Assert.False(result.IsSuccess);
        Assert.Equal(1, featureProvider.FreshChecks);
        Assert.Contains("different backend context", result.Failure!.Errors.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, backend.ApiRequests);
    }

    [Theory]
    [InlineData("""{"type":"Elsa.CompensationScope","version":1}""")]
    [InlineData("""{"type":"Elsa.NotFoundActivity","originalActivityJson":"{\"type\":\"Elsa.CompensationScope\",\"version\":9}"}""")]
    [InlineData("""{"type":"Elsa.NotFoundActivity","missingTypeName":"Elsa.CompensationScope"}""")]
    public void ContainsCompensationScope_RecognizesTypedAndOpaqueScopes(string json)
    {
        Assert.True(NativeCompensationDraftCapability.ContainsCompensationScope(System.Text.Json.Nodes.JsonNode.Parse(json)));
    }

    private sealed class FreshFeatureProvider(bool enabled) : IRemoteFeatureProvider, IRemoteFeatureProviderWithFreshCheck
    {
        public string? LastFeatureName { get; private set; }
        public int FreshChecks { get; private set; }

        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>>([]);

        public Task<bool> IsEnabledFreshAsync(string featureName, CancellationToken cancellationToken = default)
        {
            LastFeatureName = featureName;
            FreshChecks++;
            return Task.FromResult(enabled);
        }
    }

    private sealed class CachedOnlyFeatureProvider : IRemoteFeatureProvider
    {
        public int CachedChecks { get; private set; }

        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
        {
            CachedChecks++;
            return Task.FromResult(true);
        }

        public Task<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>>([]);
    }

    private sealed class ContextBackendApiClientProvider(Uri url, string contextKey) : IBackendApiClientProvider
    {
        public Uri Url { get; } = url;
        public string ContextKey { get; } = contextKey;
        public int ApiRequests { get; private set; }

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class
        {
            ApiRequests++;
            throw new InvalidOperationException("A context-bound draft refusal must not create a backend API client.");
        }
    }

    private class CompletedMediator : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Task.CompletedTask;
    }
}
