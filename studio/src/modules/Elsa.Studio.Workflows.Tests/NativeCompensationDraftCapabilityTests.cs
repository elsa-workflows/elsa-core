using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Notifications;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Services;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptors.Responses;
using Elsa.Studio.Workflows.DiagramDesigners.CompensationScopes;
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
    public async Task FreshCapabilityResultIsRejectedIfBackendChangesDuringTheRequest()
    {
        var backend = new ContextBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var featureProvider = new DelayedFreshFeatureProvider();
        var check = NativeCompensationDraftCapability.IsEnabledForContextAsync(featureProvider, backend, "context-A");

        await featureProvider.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        backend.ContextKey = "context-B";
        featureProvider.Complete(true);

        Assert.False(await check);
    }

    [Fact]
    public async Task SaveNativeDraft_RejectsSameUrlContextSwitchEvenWhenNewContextAdvertisesFeature()
    {
        const string backendUrl = "https://backend.example/";
        var backend = new ContextBackendApiClientProvider(new Uri(backendUrl), "environment-B|" + backendUrl);
        var mediator = DispatchProxy.Create<IMediator, CompletedMediator>();
        var featureProvider = new FreshFeatureProvider(true);
        var service = new WorkflowDefinitionEditorService(backend, mediator, featureProvider);
        var definition = CreateDraft();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "environment-A|" + backendUrl));

        var result = await service.SaveNativeCompensationDraftAsync(definition, false, "environment-A|" + backendUrl);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, featureProvider.FreshChecks);
        Assert.Contains("different backend context", result.Failure!.Errors.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, backend.ApiRequests);
    }

    [Fact]
    public async Task UnboundDraftSaveAsyncCannotBypassContextBoundSavePath()
    {
        var backend = new ContextBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var featureProvider = new FreshFeatureProvider(true);
        var service = new WorkflowDefinitionEditorService(backend, DispatchProxy.Create<IMediator, CompletedMediator>(), featureProvider);

        var result = await service.SaveAsync(CreateDraft(), false);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, backend.ApiRequests);
        Assert.Equal(0, featureProvider.FreshChecks);
        Assert.Contains("backend context", result.Failure!.Errors.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CloneBoundToContextAIsNotSavedThroughUnboundSaveAsyncAfterSwitchToB()
    {
        var backend = new ContextBackendApiClientProvider(new Uri("https://backend.example/"), "context-B");
        var featureProvider = new FreshFeatureProvider(true);
        var service = new WorkflowDefinitionEditorService(backend, DispatchProxy.Create<IMediator, CompletedMediator>(), featureProvider);
        var original = CreateDraft();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(original, "context-A"));
        var clone = new WorkflowDefinition { Root = original.Root };

        Assert.True(NativeCompensationDraftDocumentContext.Copy(original, clone));
        var result = await service.SaveAsync(clone, false);

        Assert.False(result.IsSuccess);
        Assert.Equal("context-A", NativeCompensationDraftDocumentContext.GetBackendContextKey(clone));
        Assert.Equal(0, backend.ApiRequests);
    }

    [Fact]
    public async Task SaveRejectsContextSwitchDuringMediatorAwaitBeforeWriting()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        backend.SetApi(api);
        var mediator = DispatchProxy.Create<IMediator, GatedMediator>();
        var gate = (GatedMediator)(object)mediator;
        var service = new WorkflowDefinitionEditorService(backend, mediator, new FreshFeatureProvider(true));
        var definition = CreateDraft();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "context-A"));

        var save = service.SaveAsync(definition, false);
        await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        backend.ContextKey = "context-B";
        gate.Release.TrySetResult(true);

        var result = await save;
        Assert.False(result.IsSuccess);
        Assert.Equal(0, ((RecordingWorkflowDefinitionsApi)(object)api).SaveCalls);
    }

    [Fact]
    public async Task SaveRejectsContextSwitchDuringApiAcquisitionBeforeMediatorOrWrite()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        var mediator = DispatchProxy.Create<IMediator, CompletedMediator>();
        var service = new WorkflowDefinitionEditorService(backend, mediator, new FreshFeatureProvider(true));
        var definition = CreateDraft();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "context-A"));

        var save = service.SaveAsync(definition, false);
        await backend.ApiRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        backend.ContextKey = "context-B";
        backend.SetApi(api);

        var result = await save;
        Assert.False(result.IsSuccess);
        Assert.Equal(0, ((RecordingWorkflowDefinitionsApi)(object)api).SaveCalls);
    }

    [Fact]
    public async Task SaveRejectsContextSwitchWhileFreshCapabilityIsPendingBeforeClientOrWrite()
    {
        var backend = new ContextBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var featureProvider = new DelayedFreshFeatureProvider();
        var service = new WorkflowDefinitionEditorService(backend, DispatchProxy.Create<IMediator, CompletedMediator>(), featureProvider);
        var definition = CreateDraft();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "context-A"));

        var save = service.SaveAsync(definition, false);
        await featureProvider.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        backend.ContextKey = "context-B";
        featureProvider.Complete(true);

        var result = await save;
        Assert.False(result.IsSuccess);
        Assert.Equal(0, backend.ApiRequests);
        Assert.Contains("different backend context", result.Failure!.Errors.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublishRejectsContextSwitchDuringPublishingNotificationBeforeApiWrite()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        backend.SetApi(api);
        var mediator = DispatchProxy.Create<IMediator, GatedMediator>();
        var gate = (GatedMediator)(object)mediator;
        var service = new RemoteWorkflowDefinitionService(backend, null!, null!, mediator, new FreshFeatureProvider(true));

        var publish = service.PublishAsync("definition-1");
        await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        backend.ContextKey = "context-B";
        gate.Release.TrySetResult(true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => publish);
        Assert.Equal(0, ((RecordingWorkflowDefinitionsApi)(object)api).PublishCalls);
    }

    [Fact]
    public async Task BulkPublishRejectsContextSwitchDuringPublishingNotificationBeforeApiWrite()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        backend.SetApi(api);
        var mediator = DispatchProxy.Create<IMediator, GatedMediator>();
        var gate = (GatedMediator)(object)mediator;
        var service = new RemoteWorkflowDefinitionService(backend, null!, null!, mediator, new FreshFeatureProvider(true));

        var publish = service.BulkPublishAsync(["definition-1"]);
        await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        backend.ContextKey = "context-B";
        gate.Release.TrySetResult(true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => publish);
        Assert.Equal(0, ((RecordingWorkflowDefinitionsApi)(object)api).BulkPublishCalls);
    }

    [Fact]
    public async Task ActivityRegistryDiscardsDescriptorsWhenCapabilityCheckCompletesAfterContextSwitch()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IActivityDescriptorsApi, RecordingActivityDescriptorsApi>();
        backend.SetApi(api);
        var featureProvider = new DelayedFreshFeatureProvider();
        var provider = new RemoteActivityRegistryProvider(backend, featureProvider);

        var load = provider.ListAsync();
        await featureProvider.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        backend.ContextKey = "context-B";
        featureProvider.Complete(true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => load);
        Assert.Equal(1, ((RecordingActivityDescriptorsApi)(object)api).ListCalls);
    }

    [Fact]
    public async Task LateDefinitionResponseFromContextAIsDiscardedAfterSwitchToB()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, DelayedGetWorkflowDefinitionsApi>();
        backend.SetApi(api);
        var service = new RemoteWorkflowDefinitionService(
            backend,
            null!,
            null!,
            DispatchProxy.Create<IMediator, CompletedMediator>(),
            new FreshFeatureProvider(true));
        var definition = CreateDraft();
        var apiProxy = (DelayedGetWorkflowDefinitionsApi)(object)api;

        var load = service.FindByDefinitionIdAsync("definition-1");
        await apiProxy.GetStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        backend.ContextKey = "context-B";
        apiProxy.Complete(definition);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => load);
        Assert.Null(NativeCompensationDraftDocumentContext.GetBackendContextKey(definition));
    }

    [Fact]
    public async Task ImportRejectsContextSwitchDuringApiAcquisitionBeforeCallingImport()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        var importer = new WorkflowDefinitionImporter(
            backend,
            DispatchProxy.Create<IMediator, CompletedMediator>(),
            new AnyWorkflowJsonDetector(),
            new FreshFeatureProvider(true));
        const string workflowJson = """
            {"root":{"type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}}
            """;

        var import = importer.ImportJsonAsync("draft.json", workflowJson, null);
        await backend.ApiRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        backend.ContextKey = "context-B";
        backend.SetApi(api);

        var result = await import;
        Assert.NotNull(result.Failure);
        Assert.Equal(0, ((RecordingWorkflowDefinitionsApi)(object)api).ImportCalls);
    }

    [Theory]
    [InlineData("""{"type":"Elsa.CompensationScope","version":1}""")]
    [InlineData("""{"type":"Elsa.NotFoundActivity","originalActivityJson":"{\"type\":\"Elsa.CompensationScope\",\"version\":9}"}""")]
    [InlineData("""{"type":"Elsa.NotFoundActivity","missingTypeName":"Elsa.CompensationScope"}""")]
    public void ContainsCompensationScope_RecognizesTypedAndOpaqueScopes(string json)
    {
        Assert.True(NativeCompensationDraftCapability.ContainsCompensationScope(System.Text.Json.Nodes.JsonNode.Parse(json)));
    }

    [Fact]
    public void HasSupportedDraftShapeRejectsDuplicateStepIdsWithoutRepairingThem()
    {
        var scope = System.Text.Json.Nodes.JsonNode.Parse("""
            {"type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[{"stepId":"same"},{"stepId":"same"}]}
            """)!;

        Assert.False(NativeCompensationDraftCapability.HasSupportedDraftShape(scope));
        Assert.Equal("same", scope["steps"]![0]!["stepId"]!.GetValue<string>());
        Assert.Equal("same", scope["steps"]![1]!["stepId"]!.GetValue<string>());
    }

    [Fact]
    public void ChildInputExpressionNamedLiteralIsNotTreatedAsAnActivityPin()
    {
        var descriptor = new ActivityDescriptor
        {
            TypeName = "Elsa.WriteLine",
            Version = 1,
            Inputs = [new() { Name = "Text" }]
        };
        var registry = new TestActivityRegistry(descriptor);
        var activity = System.Text.Json.Nodes.JsonNode.Parse("""
            {"id":"write-1","type":"Elsa.WriteLine","version":1,"text":{"typeName":"String","expression":{"type":"Literal","value":"hello"}}}
            """)!.AsObject();

        Assert.True(CompensationScopeDesigner.IsSupportedPinnedActivity(activity, registry));
    }

    [Fact]
    public void ExistingPublicServiceConstructorSignaturesRemainAvailable()
    {
        Assert.NotNull(typeof(RemoteActivityRegistryProvider).GetConstructor([typeof(IBackendApiClientProvider)]));
        Assert.NotNull(typeof(WorkflowDefinitionImporter).GetConstructor([typeof(IBackendApiClientProvider), typeof(IMediator), typeof(IWorkflowJsonDetector)]));
        Assert.Contains(typeof(RemoteWorkflowDefinitionService).GetConstructors(), constructor =>
        {
            var parameters = constructor.GetParameters();
            return parameters.Length == 4 &&
                   parameters[0].ParameterType == typeof(IBackendApiClientProvider) &&
                   parameters[1].ParameterType.FullName == "Elsa.Studio.Workflows.Domain.Contracts.IIdentityGenerator" &&
                   parameters[2].ParameterType == typeof(IWorkflowRootActivityTemplateProvider) &&
                   parameters[3].ParameterType == typeof(IMediator);
        });
    }

    private static WorkflowDefinition CreateDraft() => new()
    {
        Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}""")!.AsObject()
    };

    private sealed class AnyWorkflowJsonDetector : IWorkflowJsonDetector
    {
        public bool IsWorkflowSchema(string json) => true;
    }

    private sealed class TestActivityRegistry(ActivityDescriptor descriptor) : IActivityRegistry
    {
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<ActivityDescriptor> List() => [descriptor];
        public ActivityDescriptor? Find(string activityType, int? version = default) =>
            activityType == descriptor.TypeName && version == descriptor.Version ? descriptor : null;
        public IEnumerable<ActivityDescriptor> FindAll(string activityType) => activityType == descriptor.TypeName ? [descriptor] : [];
        public void MarkStale() { }
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

    private sealed class DelayedFreshFeatureProvider : IRemoteFeatureProvider, IRemoteFeatureProviderWithFreshCheck
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>>([]);

        public Task<bool> IsEnabledFreshAsync(string featureName, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            return _result.Task;
        }

        public void Complete(bool enabled) => _result.TrySetResult(enabled);
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
        public string ContextKey { get; set; } = contextKey;
        public int ApiRequests { get; private set; }

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class
        {
            ApiRequests++;
            throw new InvalidOperationException("A context-bound draft refusal must not create a backend API client.");
        }
    }

    private sealed class TestBackendApiClientProvider(Uri url, string contextKey) : IBackendApiClientProvider
    {
        private readonly TaskCompletionSource<object> _api = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Uri Url { get; } = url;
        public string ContextKey { get; set; } = contextKey;
        public TaskCompletionSource<bool> ApiRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void SetApi(object api) => _api.TrySetResult(api);

        public async ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class
        {
            ApiRequested.TrySetResult(true);
            return (T)await _api.Task.WaitAsync(cancellationToken);
        }
    }

    private class GatedMediator : DispatchProxy
    {
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => WaitAndCompleteAsync();

        private async Task WaitAndCompleteAsync()
        {
            Started.TrySetResult(true);
            await Release.Task;
        }
    }

    private class RecordingWorkflowDefinitionsApi : DispatchProxy
    {
        public int SaveCalls { get; private set; }
        public int ImportCalls { get; private set; }
        public int PublishCalls { get; private set; }
        public int BulkPublishCalls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IWorkflowDefinitionsApi.ImportAsync))
            {
                ImportCalls++;
                return Task.FromResult(new WorkflowDefinition());
            }

            if (targetMethod?.Name == nameof(IWorkflowDefinitionsApi.PublishAsync))
            {
                PublishCalls++;
                return Task.FromException<SaveWorkflowDefinitionResponse>(new InvalidOperationException("Unexpected workflow publication."));
            }

            if (targetMethod?.Name == nameof(IWorkflowDefinitionsApi.BulkPublishAsync))
            {
                BulkPublishCalls++;
                return Task.FromException<BulkPublishWorkflowDefinitionsResponse>(new InvalidOperationException("Unexpected workflow publication."));
            }

            if (targetMethod?.Name != nameof(IWorkflowDefinitionsApi.SaveAsync))
                throw new NotSupportedException(targetMethod?.Name);

            SaveCalls++;
            var request = (SaveWorkflowDefinitionRequest)args![0]!;
            var definition = new WorkflowDefinition { Root = request.Model.Root! };
            return Task.FromResult(new SaveWorkflowDefinitionResponse(definition, false, 0));
        }
    }

    private class DelayedGetWorkflowDefinitionsApi : DispatchProxy
    {
        private readonly TaskCompletionSource<WorkflowDefinition?> _definition = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> GetStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IWorkflowDefinitionsApi.GetByDefinitionIdAsync))
                throw new NotSupportedException(targetMethod?.Name);
            GetStarted.TrySetResult(true);
            return _definition.Task;
        }

        public void Complete(WorkflowDefinition definition) => _definition.TrySetResult(definition);
    }

    private class RecordingActivityDescriptorsApi : DispatchProxy
    {
        public int ListCalls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IActivityDescriptorsApi.ListAsync))
                throw new NotSupportedException(targetMethod?.Name);

            ListCalls++;
            return Task.FromResult(new ListActivityDescriptorsResponse([], 0));
        }
    }

    private class CompletedMediator : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Task.CompletedTask;
    }
}
