using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity;
using Elsa.Studio.Authentication.ElsaIdentity.HttpMessageHandlers;
using Elsa.Studio.Authentication.ElsaIdentity.Services;
using Elsa.Studio.Services;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Notifications;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Services;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptors.Responses;
using Elsa.Studio.Workflows.DiagramDesigners.CompensationScopes;
using Elsa.Studio.Workflows.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Refit;
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
    public async Task NativeCapabilityRequiresAnAuthenticatedRequestContextBinding()
    {
        var backend = new ContextBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");

        Assert.False(await NativeCompensationDraftCapability.IsEnabledForContextAsync(
            new FreshFeatureProvider(true), backend, backend.ContextKey));
    }

    [Theory]
    [InlineData("application/json", false)]
    [InlineData("application/problem+json", true)]
    public async Task NativeDraftSavePreservesNativeCodeFromRefitTypedAndUntypedCodeOnlyFailures(string contentType, bool isValidationApiException)
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, NativeRefusalWorkflowDefinitionsApi>();
        var apiProxy = (NativeRefusalWorkflowDefinitionsApi)(object)api;
        apiProxy.RefusalException = await CreateRefusalApiExceptionAsync(contentType);
        Assert.Equal(isValidationApiException, apiProxy.RefusalException is ValidationApiException);
        backend.SetApi(api);
        var service = new WorkflowDefinitionEditorService(backend, DispatchProxy.Create<IMediator, CompletedMediator>(), new FreshFeatureProvider(true));
        var definition = CreateDraft();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, backend.ContextKey));

        var result = await service.SaveNativeCompensationDraftAsync(definition, true, backend.ContextKey);

        Assert.False(result.IsSuccess);
        Assert.Equal(NativeCompensationDraftRefusal.Code, result.Failure!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, result.Failure.StatusCode);
        Assert.Empty(result.Failure.Errors);
        Assert.Empty(NativeCompensationDraftRefusal.FromValidationErrors(result.Failure)!.Diagnostics);
        Assert.Equal(1, apiProxy.SaveCalls);
    }

    [Fact]
    public async Task NativeDraftSaveWithLegacyProviderRefusesBeforeCreatingAWriteClient()
    {
        var backend = new ContextBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var featureProvider = new FreshFeatureProvider(true);
        var service = new WorkflowDefinitionEditorService(backend, DispatchProxy.Create<IMediator, CompletedMediator>(), featureProvider);
        var definition = CreateDraft();
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "context-A"));

        var result = await service.SaveAsync(definition, false);

        Assert.False(result.IsSuccess);
        Assert.Equal(1, featureProvider.FreshChecks);
        Assert.Equal(0, backend.ApiRequests);
        Assert.Contains("native compensation draft authoring", result.Failure!.Errors.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
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
    public async Task SaveSendsTheSameOrdinaryRootSnapshotThatWasClassifiedBeforeMediatorAwait()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        backend.SetApi(api);
        var mediator = DispatchProxy.Create<IMediator, GatedMediator>();
        var gate = (GatedMediator)(object)mediator;
        var service = new WorkflowDefinitionEditorService(backend, mediator, new FreshFeatureProvider(true));
        var originalRoot = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"write-1","type":"Elsa.WriteLine","version":1}""")!.AsObject();
        var definition = new WorkflowDefinition { Root = originalRoot };
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "context-A"));

        var save = service.SaveAsync(definition, false);
        await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        backend.ContextKey = "context-B";
        definition.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}""")!.AsObject();
        gate.Release.TrySetResult(true);

        var result = await save;

        Assert.True(result.IsSuccess);
        Assert.Equal(originalRoot.ToJsonString(), ((RecordingWorkflowDefinitionsApi)(object)api).SavedRoot!.ToJsonString());
        Assert.False(NativeCompensationDraftCapability.ContainsCompensationScope(((RecordingWorkflowDefinitionsApi)(object)api).SavedRoot));
    }

    [Fact]
    public async Task LegacyEditorServiceConstructorSavesScopeLikeOrdinaryInputThroughOrdinaryRoute()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        backend.SetApi(api);
        var definition = new WorkflowDefinition
        {
            Root = Support.NativeCompensationDraftTestData.CreateLegacySetVariableWithScopeLikeLiteralAndMetadata()
        };
        var originalRoot = definition.Root!.ToJsonString();
        var service = new WorkflowDefinitionEditorService(backend, DispatchProxy.Create<IMediator, CompletedMediator>());

        var result = await service.SaveAsync(definition, false);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, ((RecordingWorkflowDefinitionsApi)(object)api).SaveCalls);
        Assert.Equal(originalRoot, ((RecordingWorkflowDefinitionsApi)(object)api).SavedRoot!.ToJsonString());
        Assert.False(backend.HasActiveRequestContextBinding);
    }

    [Theory]
    [InlineData("""{"id":"sequence-1","type":"Elsa.Sequence","version":1,"activities":[{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}] }""")]
    [InlineData("""{"id":"flowchart-1","type":"Elsa.Flowchart","version":1,"activities":{"scope-1":{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}} }""")]
    [InlineData("""{"id":"if-1","type":"Elsa.If","version":1,"then":{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]} }""")]
    public async Task LegacyEditorServiceConstructorRejectsNestedNativeScopeBeforeSave(string rootJson)
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        backend.SetApi(api);
        var definition = new WorkflowDefinition { Root = System.Text.Json.Nodes.JsonNode.Parse(rootJson)!.AsObject() };
        var service = new WorkflowDefinitionEditorService(backend, DispatchProxy.Create<IMediator, CompletedMediator>());

        var result = await service.SaveAsync(definition, false);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, ((RecordingWorkflowDefinitionsApi)(object)api).SaveCalls);
    }

    [Fact]
    public async Task LegacyRemoteServiceConstructorRejectsNestedNativeScopeBeforePublishing()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        ((RecordingWorkflowDefinitionsApi)(object)api).StoredDefinition = new()
        {
            DefinitionId = "definition-1",
            Root = System.Text.Json.Nodes.JsonNode.Parse("""
                {"id":"sequence-1","type":"Elsa.Sequence","version":1,"activities":[{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}]}
                """)!.AsObject()
        };
        backend.SetApi(api);
        var service = new RemoteWorkflowDefinitionService(backend, null!, null!, DispatchProxy.Create<IMediator, CompletedMediator>());

        await Assert.ThrowsAsync<NativeCompensationDraftPublishUnavailableException>(() => service.PublishAsync("definition-1"));

        Assert.Equal(0, ((RecordingWorkflowDefinitionsApi)(object)api).PublishCalls);
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
            {"root":{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}}
            """;

        var import = importer.ImportJsonAsync("draft.json", workflowJson, null);
        await backend.ApiRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        backend.ContextKey = "context-B";
        backend.SetApi(api);

        var result = await import;
        Assert.NotNull(result.Failure);
        Assert.Equal(0, ((RecordingWorkflowDefinitionsApi)(object)api).ImportCalls);
    }

    [Fact]
    public async Task ImportReclassifiesMediatorRootMutationAndSendsItsFrozenSnapshot()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        var apiProxy = (RecordingWorkflowDefinitionsApi)(object)api;
        apiProxy.IsRequestContextBound = () => backend.HasActiveRequestContextBinding;
        backend.SetApi(api);
        var mediator = DispatchProxy.Create<IMediator, MutatingImportMediator>();
        var mediatorProxy = (MutatingImportMediator)(object)mediator;
        var featureProvider = new DelayedFreshFeatureProvider();
        var importer = new WorkflowDefinitionImporter(backend, mediator, new AnyWorkflowJsonDetector(), featureProvider);
        const string workflowJson = """
            {"name":"original","root":{"id":"write-1","type":"Elsa.WriteLine","version":1}}
            """;

        var import = importer.ImportJsonAsync("draft.json", workflowJson, null);
        await featureProvider.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        mediatorProxy.Model!.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"late-write-1","type":"Elsa.WriteLine","version":1}""")!.AsObject();
        mediatorProxy.Model.Name = "late-name";
        featureProvider.Complete(true);

        var result = await import;

        Assert.Null(result.Failure);
        Assert.Equal(1, apiProxy.ImportCalls);
        Assert.Equal("event-name", apiProxy.ImportedName);
        Assert.True(apiProxy.ImportWasBound);
        Assert.True(NativeCompensationDraftCapability.ContainsCompensationScope(apiProxy.ImportedRoot));
        Assert.Equal("Elsa.CompensationScope", apiProxy.ImportedRoot!["type"]!.GetValue<string>());
    }

    [Fact]
    public async Task CreateNewDefinitionSendsTheConfiguredRootSnapshotAfterApiAcquisition()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        var apiProxy = (RecordingWorkflowDefinitionsApi)(object)api;
        var service = new RemoteWorkflowDefinitionService(
            backend,
            new RandomLongIdentityGenerator(),
            new DefaultWorkflowRootActivityTemplateProvider(),
            DispatchProxy.Create<IMediator, CompletedMediator>(),
            new FreshFeatureProvider(false));
        SaveWorkflowDefinitionRequest? configuredRequest = null;

        var create = service.CreateNewDefinitionAsync("initial", configureRequest: request =>
        {
            request.Model.Name = "configured-name";
            request.Model.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"write-1","type":"Elsa.WriteLine","version":1}""")!.AsObject();
            configuredRequest = request;
        });
        await backend.ApiRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        configuredRequest!.Model.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}""")!.AsObject();
        backend.SetApi(api);

        var result = await create;

        Assert.True(result.IsSuccess);
        Assert.Equal("configured-name", apiProxy.SavedName);
        Assert.False(NativeCompensationDraftCapability.ContainsCompensationScope(apiProxy.SavedRoot));
        Assert.Equal("Elsa.WriteLine", apiProxy.SavedRoot!["type"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("import")]
    [InlineData("create")]
    [InlineData("publish")]
    [InlineData("bulk-publish")]
    [InlineData("stored-publish")]
    [InlineData("stored-bulk-publish")]
    public async Task NativeWriteUsesTheAuthenticatedTransportBindingBeforeTokenSwitchDispatch(string operation)
    {
        using var harness = await AuthenticatedNativeRequestHarness.CreateAsync();
        var backend = harness.BackendApiClientProvider;
        var mediator = DispatchProxy.Create<IMediator, CompletedMediator>();
        var featureProvider = new FreshFeatureProvider(operation is not ("stored-publish" or "stored-bulk-publish"));
        var service = new RemoteWorkflowDefinitionService(
            backend,
            new RandomLongIdentityGenerator(),
            new DefaultWorkflowRootActivityTemplateProvider(),
            mediator,
            featureProvider);

        switch (operation)
        {
            case "import":
                var importer = new WorkflowDefinitionImporter(backend, mediator, new AnyWorkflowJsonDetector(), featureProvider);
                var importResult = await importer.ImportJsonAsync("draft.json", """
                    {"root":{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}}
                    """, null);
                Assert.NotNull(importResult.Failure);
                break;
            case "create":
                await Assert.ThrowsAsync<BackendRequestContextChangedException>(() => service.CreateNewDefinitionAsync(
                    "native draft",
                    configureRequest: request => request.Model.Root = System.Text.Json.Nodes.JsonNode.Parse(
                        """{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}""")!.AsObject()));
                break;
            case "publish":
                await Assert.ThrowsAsync<BackendRequestContextChangedException>(() => service.PublishAsync("definition-1"));
                break;
            case "bulk-publish":
                await Assert.ThrowsAsync<BackendRequestContextChangedException>(() => service.BulkPublishAsync(["definition-1"]));
                break;
            case "stored-publish":
                await Assert.ThrowsAsync<BackendRequestContextChangedException>(() => service.PublishAsync("definition-1"));
                break;
            case "stored-bulk-publish":
                await Assert.ThrowsAsync<BackendRequestContextChangedException>(() => service.BulkPublishAsync(["definition-1"]));
                break;
        }

        Assert.Equal(1, harness.PrimeTransport.RequestCount);
        Assert.Equal(0, harness.ApiTransport.RequestCount);
        Assert.Equal(1, harness.RefreshTokenService.RefreshCalls);
    }

    [Fact]
    public async Task StoredNativePublishRefusalUsesTheFeatureOwnedSignal()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        ((RecordingWorkflowDefinitionsApi)(object)api).StoredDefinition = CreateDraft();
        backend.SetApi(api);
        var service = new RemoteWorkflowDefinitionService(
            backend,
            null!,
            null!,
            DispatchProxy.Create<IMediator, CompletedMediator>(),
            new FreshFeatureProvider(false));

        await Assert.ThrowsAsync<NativeCompensationDraftPublishUnavailableException>(() => service.PublishAsync("definition-1"));
        Assert.Equal(0, ((RecordingWorkflowDefinitionsApi)(object)api).PublishCalls);
    }

    [Fact]
    public async Task BulkStoredNativePublishRefusalUsesTheFeatureOwnedSignal()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        ((RecordingWorkflowDefinitionsApi)(object)api).StoredDefinitionsByDefinitionId = new Dictionary<string, WorkflowDefinition>(StringComparer.Ordinal)
        {
            ["definition-1"] = new() { Id = "definition-1:4", DefinitionId = "definition-1", Root = CreateDraft().Root }
        };
        backend.SetApi(api);
        var service = new RemoteWorkflowDefinitionService(
            backend,
            null!,
            null!,
            DispatchProxy.Create<IMediator, CompletedMediator>(),
            new FreshFeatureProvider(false));

        await Assert.ThrowsAsync<NativeCompensationDraftPublishUnavailableException>(() => service.BulkPublishAsync(["definition-1"]));
        Assert.Equal(0, ((RecordingWorkflowDefinitionsApi)(object)api).BulkPublishCalls);
    }

    [Fact]
    public async Task LegacyBulkPublishContinuesPastMissingTargetAndPreservesCanonicalNotFoundResult()
    {
        var (backend, api, service) = CreateLegacyBulkPublishHarness();
        api.StoredReadExceptionsByDefinitionId["missing"] = await CreateApiExceptionAsync(HttpStatusCode.NotFound);
        api.StoredDefinitionsByDefinitionId["ordinary"] = new()
        {
            Id = "ordinary:3",
            DefinitionId = "ordinary",
            Root = System.Text.Json.Nodes.JsonNode.Parse("""{"type":"Elsa.WriteLine","version":1}""")!.AsObject()
        };
        api.BulkPublishResponse = new([], [], ["missing"], []);
        var definitionIds = new[] { "missing", "ordinary" };

        var response = await service.BulkPublishAsync(definitionIds);

        Assert.Equal(definitionIds, api.DefinitionIdReads);
        Assert.Equal(definitionIds, api.BulkPublishRequestIds);
        Assert.Equal(1, api.BulkPublishCalls);
        Assert.Equal("missing", response.NotFound.Single());
        Assert.All(api.StoredReadBindingStates, Assert.True);
        Assert.True(api.BulkPublishWasBound);
        Assert.False(backend.HasActiveRequestContextBinding);
    }

    [Fact]
    public async Task LegacyBulkPublishStopsWhenNativeTargetFollowsMissingTarget()
    {
        var (_, api, service) = CreateLegacyBulkPublishHarness();
        api.StoredReadExceptionsByDefinitionId["missing"] = await CreateApiExceptionAsync(HttpStatusCode.NotFound);
        api.StoredDefinitionsByDefinitionId["native"] = new()
        {
            Id = "native:4",
            DefinitionId = "native",
            Root = CreateDraft().Root
        };

        await Assert.ThrowsAsync<NativeCompensationDraftPublishUnavailableException>(() =>
            service.BulkPublishAsync(["missing", "native"]));

        Assert.Equal(new[] { "missing", "native" }, api.DefinitionIdReads);
        Assert.Equal(0, api.BulkPublishCalls);
    }

    [Fact]
    public async Task CreateNativeDraftPreservesTheNativeRefusalCodeFromTheServer()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, NativeRefusalWorkflowDefinitionsApi>();
        var apiProxy = (NativeRefusalWorkflowDefinitionsApi)(object)api;
        apiProxy.RefusalException = await CreateRefusalApiExceptionAsync("application/json");
        backend.SetApi(api);
        var service = new RemoteWorkflowDefinitionService(
            backend,
            new RandomLongIdentityGenerator(),
            new DefaultWorkflowRootActivityTemplateProvider(),
            DispatchProxy.Create<IMediator, CompletedMediator>(),
            new FreshFeatureProvider(true));

        var result = await service.CreateNewDefinitionAsync("native draft", configureRequest: request =>
            request.Model.Root = System.Text.Json.Nodes.JsonNode.Parse(
                """{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}""")!.AsObject());

        Assert.False(result.IsSuccess);
        Assert.Equal(NativeCompensationDraftRefusal.Code, result.Failure!.Code);
        Assert.Empty(result.Failure.Errors);
        Assert.Equal(1, apiProxy.SaveCalls);
    }

    [Theory]
    [InlineData("publish")]
    [InlineData("bulk-publish")]
    public async Task OrdinaryStoredPublicationRemainsCompatibleWithALegacyBackendProvider(string operation)
    {
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        var apiProxy = (RecordingWorkflowDefinitionsApi)(object)api;
        var ordinaryDefinition = new WorkflowDefinition
        {
            Root = System.Text.Json.Nodes.JsonNode.Parse("""{"type":"Elsa.WriteLine","version":1}""")!.AsObject()
        };
        apiProxy.StoredDefinition = ordinaryDefinition;
        apiProxy.StoredDefinitionsByDefinitionId = new Dictionary<string, WorkflowDefinition>(StringComparer.Ordinal)
        {
            ["definition-1"] = ordinaryDefinition
        };
        apiProxy.StoredDefinitions = [ordinaryDefinition];
        var backend = new LegacyTestBackendApiClientProvider(new Uri("https://backend.example/"), "legacy-context", api);
        var service = new RemoteWorkflowDefinitionService(
            backend,
            null!,
            null!,
            DispatchProxy.Create<IMediator, CompletedMediator>(),
            new FreshFeatureProvider(false));

        if (operation == "publish")
            await service.PublishAsync("definition-1");
        else
            await service.BulkPublishAsync(["definition-1"]);

        Assert.Equal(operation == "publish" ? 1 : 0, apiProxy.PublishCalls);
        Assert.Equal(operation == "bulk-publish" ? 1 : 0, apiProxy.BulkPublishCalls);
    }

    [Theory]
    [InlineData("publish")]
    [InlineData("bulk-publish")]
    public async Task StoredPublicationDoesNotDowngradeAfterRequestBindingQualificationDrops(string operation)
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        var apiProxy = (RecordingWorkflowDefinitionsApi)(object)api;
        var ordinaryDefinition = new WorkflowDefinition
        {
            Root = System.Text.Json.Nodes.JsonNode.Parse("""{"type":"Elsa.WriteLine","version":1}""")!.AsObject()
        };
        apiProxy.StoredDefinition = ordinaryDefinition;
        apiProxy.StoredDefinitions = [ordinaryDefinition];
        apiProxy.IsRequestContextBound = () => backend.HasActiveRequestContextBinding;
        backend.SetApi(api);
        var mediator = DispatchProxy.Create<IMediator, BindingDroppingMediator>();
        ((BindingDroppingMediator)(object)mediator).Backend = backend;
        var service = new RemoteWorkflowDefinitionService(
            backend,
            null!,
            null!,
            mediator,
            new FreshFeatureProvider(false));

        if (operation == "publish")
            await Assert.ThrowsAsync<BackendRequestContextChangedException>(() => service.PublishAsync("definition-1"));
        else
            await Assert.ThrowsAsync<BackendRequestContextChangedException>(() => service.BulkPublishAsync(["definition-1"]));

        Assert.True(apiProxy.StoredReadWasBound);
        Assert.Equal(0, apiProxy.PublishCalls);
        Assert.Equal(0, apiProxy.BulkPublishCalls);
    }

    [Theory]
    [InlineData("publish")]
    [InlineData("bulk-publish")]
    public async Task PublishOnlyPermissionFallsThroughAViewForbiddenLegacyPreflight(string operation)
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        var apiProxy = (RecordingWorkflowDefinitionsApi)(object)api;
        apiProxy.IsRequestContextBound = () => backend.HasActiveRequestContextBinding;
        apiProxy.StoredReadException = await CreateApiExceptionAsync(HttpStatusCode.Forbidden);
        backend.SetApi(api);
        var service = new RemoteWorkflowDefinitionService(
            backend,
            null!,
            null!,
            DispatchProxy.Create<IMediator, CompletedMediator>(),
            new FreshFeatureProvider(false));

        if (operation == "publish")
            await service.PublishAsync("order-process");
        else
            await service.BulkPublishAsync(["order-process", "shipping-process"]);

        Assert.Equal(operation == "publish" ? 1 : 2, apiProxy.DefinitionIdReads.Count);
        Assert.Empty(apiProxy.VersionIdReads);
        Assert.Equal(operation == "publish" ? 1 : 0, apiProxy.PublishCalls);
        Assert.Equal(operation == "bulk-publish" ? 1 : 0, apiProxy.BulkPublishCalls);
        Assert.All(apiProxy.StoredReadBindingStates, Assert.True);
        if (operation == "publish")
            Assert.True(apiProxy.PublishWasBound);
        else
            Assert.True(apiProxy.BulkPublishWasBound);
    }

    [Fact]
    public async Task BulkStoredNativePreflightUsesLogicalDefinitionIdsAndChecksEveryTarget()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        var apiProxy = (RecordingWorkflowDefinitionsApi)(object)api;
        apiProxy.StoredDefinitionsByDefinitionId = new Dictionary<string, WorkflowDefinition>(StringComparer.Ordinal)
        {
            ["order-process"] = new()
            {
                Id = "order-process:7",
                DefinitionId = "order-process",
                Root = System.Text.Json.Nodes.JsonNode.Parse("""{"type":"Elsa.WriteLine","version":1}""")!.AsObject()
            },
            ["shipping-process"] = new() { Id = "shipping-process:12", DefinitionId = "shipping-process", Root = CreateDraft().Root }
        };
        backend.SetApi(api);
        var service = new RemoteWorkflowDefinitionService(
            backend,
            null!,
            null!,
            DispatchProxy.Create<IMediator, CompletedMediator>(),
            new FreshFeatureProvider(false));

        await Assert.ThrowsAsync<NativeCompensationDraftPublishUnavailableException>(() =>
            service.BulkPublishAsync(["order-process", "shipping-process"]));

        Assert.Equal(new[] { "order-process", "shipping-process" }, apiProxy.DefinitionIdReads);
        Assert.Empty(apiProxy.VersionIdReads);
        Assert.Equal(0, apiProxy.BulkPublishCalls);
    }

    [Fact]
    public async Task LegacyViewPreflightDoesNotSwallowUnauthorizedResponses()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        var apiProxy = (RecordingWorkflowDefinitionsApi)(object)api;
        var unauthorized = await CreateApiExceptionAsync(HttpStatusCode.Unauthorized);
        apiProxy.StoredReadException = unauthorized;
        backend.SetApi(api);
        var service = new RemoteWorkflowDefinitionService(
            backend,
            null!,
            null!,
            DispatchProxy.Create<IMediator, CompletedMediator>(),
            new FreshFeatureProvider(false));

        var thrown = await Assert.ThrowsAsync<ApiException>(() => service.PublishAsync("order-process"));

        Assert.Same(unauthorized, thrown);
        Assert.Equal(0, apiProxy.PublishCalls);
    }

    private (TestBackendApiClientProvider Backend, RecordingWorkflowDefinitionsApi Api, RemoteWorkflowDefinitionService Service) CreateLegacyBulkPublishHarness()
    {
        var backend = new TestBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RecordingWorkflowDefinitionsApi>();
        var apiProxy = (RecordingWorkflowDefinitionsApi)(object)api;
        apiProxy.IsRequestContextBound = () => backend.HasActiveRequestContextBinding;
        backend.SetApi(api);
        var service = new RemoteWorkflowDefinitionService(
            backend,
            null!,
            null!,
            DispatchProxy.Create<IMediator, CompletedMediator>(),
            new FreshFeatureProvider(false));
        return (backend, apiProxy, service);
    }

    [Theory]
    [InlineData("""{"type":"Elsa.CompensationScope","version":1}""")]
    [InlineData("""{"type":"Elsa.NotFoundActivity","originalActivityJson":"{\"type\":\"Elsa.CompensationScope\",\"version\":9}"}""")]
    [InlineData("""{"type":"Elsa.NotFoundActivity","originalActivityJson":"{\"type\":\"Elsa.NotFoundActivity\",\"originalActivityJson\":\"{\\\"type\\\":\\\"Elsa.CompensationScope\\\",\\\"version\\\":9}\"}"}""")]
    [InlineData("""{"type":"Elsa.NotFoundActivity","missingTypeName":"Elsa.CompensationScope"}""")]
    public void ContainsCompensationScope_RecognizesTypedAndOpaqueScopes(string json)
    {
        Assert.True(NativeCompensationDraftCapability.ContainsCompensationScope(System.Text.Json.Nodes.JsonNode.Parse(json)));
    }

    [Theory]
    [InlineData("""{"type":"Elsa.NotFoundActivity","originalActivityJson":"{\"type\":\"Elsa.NotFoundActivity\",\"originalActivityJson\":\"{\\\"type\\\":\\\"Elsa.CompensationScope\\\",\\\"version\\\":9}\"}"}""")]
    [InlineData("""{"type":"Elsa.NotFoundActivity","missingTypeName":"Elsa.CompensationScope"}""")]
    [InlineData("""{"type":"Elsa.CompensationScope","version":9,"draftSchemaVersion":1,"steps":[]}""")]
    public async Task SaveRefusesOpaqueAndUnsupportedNativeScopeBeforeOrdinarySave(string rootJson)
    {
        var backend = new ContextBackendApiClientProvider(new Uri("https://backend.example/"), "context-A");
        var featureProvider = new FreshFeatureProvider(true);
        var service = new WorkflowDefinitionEditorService(backend, DispatchProxy.Create<IMediator, CompletedMediator>(), featureProvider);
        var definition = new WorkflowDefinition
        {
            Root = System.Text.Json.Nodes.JsonNode.Parse(rootJson)!.AsObject()
        };
        Assert.True(NativeCompensationDraftDocumentContext.TryBind(definition, "context-A"));

        var result = await service.SaveAsync(definition, false);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, featureProvider.FreshChecks);
        Assert.Equal(0, backend.ApiRequests);
        Assert.Contains("cannot safely edit", result.Failure!.Errors.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("""{"type":"Elsa.NotFoundActivity","originalActivityJson":"{\"type\":\"Elsa.NotFoundActivity\",\"originalActivityJson\":\"{\\\"type\\\":\\\"Elsa.CompensationScope\\\",\\\"version\\\":9}\"}"}""")]
    [InlineData("""{"type":"Elsa.NotFoundActivity","missingTypeName":"Elsa.CompensationScope"}""")]
    public void OpaqueCompensationScopesAreSharedByTheResolverAndDesignerProvider(string json)
    {
        var activity = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();

        Assert.True(new CompensationScopeActivityResolver().GetSupportsActivity(activity));
        Assert.True(new CompensationScopeDiagramDesignerProvider().GetSupportsActivity(activity));
    }

    [Fact]
    public void OrdinaryNotFoundActivityWithScopeLikeInputDataIsNotAnOpaqueCompensationScope()
    {
        var activity = System.Text.Json.Nodes.JsonNode.Parse("""
            {"type":"Elsa.NotFoundActivity","missingTypeName":"Elsa.SetVariable","originalActivityJson":"{\"type\":\"Elsa.SetVariable\",\"version\":1,\"value\":{\"type\":\"Elsa.CompensationScope\",\"version\":1,\"draftSchemaVersion\":1,\"steps\":[]}}"}
            """)!.AsObject();

        Assert.False(NativeCompensationDraftCapability.IsOpaqueCompensationScope(activity));
        Assert.False(new CompensationScopeActivityResolver().GetSupportsActivity(activity));
    }

    [Theory]
    [InlineData("""{"type":"Elsa.CompensationScope","version":2,"draftSchemaVersion":1,"steps":[]}""")]
    [InlineData("""{"type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[{"stepId":"same"},{"stepId":"same"}]}""")]
    public void UnsupportedNativeScopeShapesAreDetectedRecursively(string json)
    {
        Assert.True(NativeCompensationDraftCapability.ContainsUnsupportedCompensationScopeShape(
            System.Text.Json.Nodes.JsonNode.Parse(json)));
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

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("\"malformed\"")]
    public void HasSupportedDraftShapeRejectsMalformedMappingsLikeCore(string mappingsJson)
    {
        var scope = System.Text.Json.Nodes.JsonNode.Parse($$"""
            {"type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[{"stepId":"step-1","mappings":{{mappingsJson}}}]}
            """)!;

        Assert.False(NativeCompensationDraftCapability.HasSupportedDraftShape(scope));
    }

    [Fact]
    public void HasSupportedDraftShapeAcceptsAbsentOrArrayMappingsLikeCore()
    {
        var withoutMappings = System.Text.Json.Nodes.JsonNode.Parse("""
            {"type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[{"stepId":"step-1"}]}
            """)!;
        var withMappings = System.Text.Json.Nodes.JsonNode.Parse("""
            {"type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[{"stepId":"step-1","mappings":[]}]}
            """)!;

        Assert.True(NativeCompensationDraftCapability.HasSupportedDraftShape(withoutMappings));
        Assert.True(NativeCompensationDraftCapability.HasSupportedDraftShape(withMappings));
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

    private static async Task<ApiException> CreateRefusalApiExceptionAsync(string contentType)
    {
        const string content = """
        {
          "errors": {},
          "code": "NATIVE_COMPENSATION_DRAFT_ONLY",
          "data": { "diagnostics": "untrusted" }
        }
        """;
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/workflow-definitions/draft-1");
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            RequestMessage = request,
            Content = new StringContent(content, Encoding.UTF8, contentType)
        };
        return await ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
    }

    private static async Task<ApiException> CreateApiExceptionAsync(HttpStatusCode statusCode)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/workflow-definitions/order-process");
        using var response = new HttpResponseMessage(statusCode) { RequestMessage = request };
        return await ApiException.Create(request, HttpMethod.Get, response, new RefitSettings());
    }

    private class NativeRefusalWorkflowDefinitionsApi : DispatchProxy
    {
        public ApiException? RefusalException { get; set; }
        public int SaveCalls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IWorkflowDefinitionsApi.SaveAsync))
                throw new NotSupportedException(targetMethod?.Name);

            SaveCalls++;
            return Task.FromException<SaveWorkflowDefinitionResponse>(RefusalException!);
        }
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

    private sealed class LegacyTestBackendApiClientProvider(Uri url, string contextKey, object api) : IBackendApiClientProvider
    {
        public Uri Url { get; } = url;
        public string ContextKey { get; } = contextKey;

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class =>
            new((T)api);
    }

    private sealed class TestBackendApiClientProvider(Uri url, string contextKey) : IBackendApiClientProvider, IBackendRequestContextBinding
    {
        private readonly TaskCompletionSource<object> _api = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly AsyncLocal<string?> _boundContext = new();
        public Uri Url { get; } = url;
        public string ContextKey { get; set; } = contextKey;
        public TaskCompletionSource<bool> ApiRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IsRequestContextBindingSupported { get; set; } = true;
        public bool SupportsAuthenticatedRequestContextBinding => IsRequestContextBindingSupported;
        public bool HasActiveRequestContextBinding => _boundContext.Value != null;

        public void RegisterAuthenticatedRequestTransport(IBackendRequestContextEpochSource? epochSource) { }

        public IDisposable BindRequestContext(string expectedContextKey)
        {
            if (!SupportsAuthenticatedRequestContextBinding || expectedContextKey != ContextKey)
                throw new BackendRequestContextChangedException();
            var previous = _boundContext.Value;
            _boundContext.Value = expectedContextKey;
            return new ActionScope(() => _boundContext.Value = previous);
        }

        public void ValidateBoundRequestContext(IBackendRequestContextEpochSource? requestEpochSource)
        {
            if (_boundContext.Value is { } expected && expected != ContextKey)
                throw new BackendRequestContextChangedException();
        }

        public void SetApi(object api) => _api.TrySetResult(api);

        public async ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class
        {
            ApiRequested.TrySetResult(true);
            return (T)await _api.Task.WaitAsync(cancellationToken);
        }

        private sealed class ActionScope(Action dispose) : IDisposable
        {
            public void Dispose() => dispose();
        }
    }

    private sealed class AuthenticatedNativeRequestHarness : IDisposable
    {
        private readonly ServiceProvider _services;
        private readonly BlazorServiceAccessor _blazorServiceAccessor;

        private AuthenticatedNativeRequestHarness(
            ServiceProvider services,
            BlazorServiceAccessor blazorServiceAccessor,
            DefaultBackendApiClientProvider backendApiClientProvider,
            MutableJwtAccessor tokenAccessor,
            SwitchingRefreshTokenService refreshTokenService,
            AuthenticatedRequestRecordingHandler primeTransport,
            AuthenticatedRequestRecordingHandler apiTransport)
        {
            _services = services;
            _blazorServiceAccessor = blazorServiceAccessor;
            BackendApiClientProvider = backendApiClientProvider;
            TokenAccessor = tokenAccessor;
            RefreshTokenService = refreshTokenService;
            PrimeTransport = primeTransport;
            ApiTransport = apiTransport;
        }

        public DefaultBackendApiClientProvider BackendApiClientProvider { get; }
        public MutableJwtAccessor TokenAccessor { get; }
        public SwitchingRefreshTokenService RefreshTokenService { get; }
        public AuthenticatedRequestRecordingHandler PrimeTransport { get; }
        public AuthenticatedRequestRecordingHandler ApiTransport { get; }

        public static async Task<AuthenticatedNativeRequestHarness> CreateAsync()
        {
            var signal = new PermissionRefreshSignal();
            var tokenAccessor = new MutableJwtAccessor(CreateToken("user-a", DateTimeOffset.UtcNow.AddHours(1)));
            var refreshTokenService = new SwitchingRefreshTokenService(tokenAccessor);
            var tokenProvider = new JwtTokenProvider(
                tokenAccessor,
                new JwtParser(),
                new ImmediateSingleFlightCoordinator(),
                refreshTokenService,
                [signal]);
            var blazorServiceAccessor = new BlazorServiceAccessor();
            var primeTransport = new AuthenticatedRequestRecordingHandler();
            var apiTransport = new AuthenticatedRequestRecordingHandler();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IPermissionRefreshSignal>(signal);
            services.AddSingleton<IRemoteBackendAccessor>(new FixedRemoteBackendAccessor());
            services.AddSingleton<IBlazorServiceAccessor>(blazorServiceAccessor);
            services.AddSingleton<ITokenProvider>(tokenProvider);
            services.AddSingleton<IBackendApiClientProvider>(serviceProvider => new DefaultBackendApiClientProvider(
                serviceProvider.GetRequiredService<IRemoteBackendAccessor>(),
                serviceProvider.GetRequiredService<IBlazorServiceAccessor>(),
                serviceProvider,
                null,
                signal));
            services.AddTransient<ElsaIdentityAuthenticatingApiHttpMessageHandler>();
            services.AddDefaultApiClients(options =>
            {
                options.BaseAddress = new Uri("https://backend.example/");
                options.AuthenticationHandler = typeof(ElsaIdentityAuthenticatingApiHttpMessageHandler);
                options.ConfigureHttpClientBuilder += builder => builder.ConfigurePrimaryHttpMessageHandler(_ => apiTransport);
                options.ConfigureRetryPolicy = null;
            });

            var serviceProvider = services.BuildServiceProvider();
            blazorServiceAccessor.Services = serviceProvider;
            var backendApiClientProvider = (DefaultBackendApiClientProvider)serviceProvider.GetRequiredService<IBackendApiClientProvider>();
            var harness = new AuthenticatedNativeRequestHarness(
                serviceProvider,
                blazorServiceAccessor,
                backendApiClientProvider,
                tokenAccessor,
                refreshTokenService,
                primeTransport,
                apiTransport);

            using (var invoker = new HttpMessageInvoker(new ElsaIdentityAuthenticatingApiHttpMessageHandler(blazorServiceAccessor)
                   {
                       InnerHandler = primeTransport
                   }))
            using (var request = new HttpRequestMessage(HttpMethod.Get, "https://backend.example/prime"))
            using (var response = await invoker.SendAsync(request, CancellationToken.None))
            {
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }

            tokenAccessor.Token = CreateToken("user-a", DateTimeOffset.UtcNow.AddSeconds(30));
            refreshTokenService.RefreshedToken = CreateToken("user-b", DateTimeOffset.UtcNow.AddHours(1));
            Assert.True(backendApiClientProvider.SupportsAuthenticatedRequestContextBinding);
            return harness;
        }

        public void Dispose()
        {
            _blazorServiceAccessor.Services = null!;
            _services.Dispose();
        }

        private static string CreateToken(string subject, DateTimeOffset expiresAt)
        {
            var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["sub"] = subject,
                ["exp"] = expiresAt.ToUnixTimeSeconds()
            });
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            return $"header.{encoded}.signature";
        }
    }

    private sealed class MutableJwtAccessor(string? token) : IJwtAccessor
    {
        public string? Token { get; set; } = token;

        public ValueTask<string?> ReadTokenAsync(string name) => ValueTask.FromResult(Token);

        public ValueTask WriteTokenAsync(string name, string token)
        {
            Token = token;
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearTokenAsync(string name)
        {
            Token = null;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SwitchingRefreshTokenService(MutableJwtAccessor tokenAccessor) : IRefreshTokenService
    {
        public string RefreshedToken { get; set; } = string.Empty;
        public int RefreshCalls { get; private set; }

        public async Task<Elsa.Api.Client.Resources.Identity.Responses.LoginResponse> RefreshTokenAsync(CancellationToken cancellationToken)
        {
            RefreshCalls++;
            await tokenAccessor.WriteTokenAsync(TokenNames.AccessToken, RefreshedToken);
            return new(true, RefreshedToken, "refresh-token");
        }
    }

    private sealed class ImmediateSingleFlightCoordinator : ISingleFlightCoordinator
    {
        public Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken) => action(cancellationToken);
    }

    private sealed class FixedRemoteBackendAccessor : IRemoteBackendAccessor
    {
        public RemoteBackend RemoteBackend { get; } = new(new Uri("https://backend.example/"));
        public string ContextKey => "tenant-a|https://backend.example/";
    }

    private sealed class AuthenticatedRequestRecordingHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent) { RequestMessage = request });
        }
    }

    private class MutatingImportMediator : DispatchProxy
    {
        public WorkflowDefinitionModel? Model { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (args?.FirstOrDefault() is ImportingWorkflowDefinition notification)
            {
                Model = notification.WorkflowDefinition;
                Model.Name = "event-name";
                Model.Root = System.Text.Json.Nodes.JsonNode.Parse("""{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}""")!.AsObject();
            }

            return Task.CompletedTask;
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

    private class BindingDroppingMediator : DispatchProxy
    {
        public TestBackendApiClientProvider Backend { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Backend.IsRequestContextBindingSupported = false;
            return Task.CompletedTask;
        }
    }

    private class RecordingWorkflowDefinitionsApi : DispatchProxy
    {
        public int SaveCalls { get; private set; }
        public int ImportCalls { get; private set; }
        public int PublishCalls { get; private set; }
        public int BulkPublishCalls { get; private set; }
        public System.Text.Json.Nodes.JsonNode? SavedRoot { get; private set; }
        public System.Text.Json.Nodes.JsonNode? ImportedRoot { get; private set; }
        public string? SavedName { get; private set; }
        public string? ImportedName { get; private set; }
        public bool ImportWasBound { get; private set; }
        public bool StoredReadWasBound { get; private set; }
        public List<bool> StoredReadBindingStates { get; } = [];
        public bool PublishWasBound { get; private set; }
        public bool BulkPublishWasBound { get; private set; }
        public Func<bool>? IsRequestContextBound { get; set; }
        public WorkflowDefinition? StoredDefinition { get; set; }
        public ICollection<WorkflowDefinition> StoredDefinitions { get; set; } = [];
        public IDictionary<string, WorkflowDefinition> StoredDefinitionsByDefinitionId { get; set; } = new Dictionary<string, WorkflowDefinition>(StringComparer.Ordinal);
        public ApiException? StoredReadException { get; set; }
        public IDictionary<string, ApiException> StoredReadExceptionsByDefinitionId { get; set; } = new Dictionary<string, ApiException>(StringComparer.Ordinal);
        public BulkPublishWorkflowDefinitionsResponse? BulkPublishResponse { get; set; }
        public List<string> BulkPublishRequestIds { get; } = [];
        public List<string> DefinitionIdReads { get; } = [];
        public List<string> VersionIdReads { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IWorkflowDefinitionsApi.GetByDefinitionIdAsync))
            {
                StoredReadWasBound = IsRequestContextBound?.Invoke() == true;
                StoredReadBindingStates.Add(StoredReadWasBound);
                var definitionId = (string)args![0]!;
                DefinitionIdReads.Add(definitionId);
                if (StoredReadExceptionsByDefinitionId.TryGetValue(definitionId, out var definitionReadException))
                    return Task.FromException<WorkflowDefinition?>(definitionReadException);
                if (StoredReadException != null)
                    return Task.FromException<WorkflowDefinition?>(StoredReadException);

                if (StoredDefinitionsByDefinitionId.TryGetValue(definitionId, out var storedDefinition))
                    return Task.FromResult<WorkflowDefinition?>(storedDefinition);

                return Task.FromResult<WorkflowDefinition?>(StoredDefinition);
            }

            if (targetMethod?.Name == nameof(IWorkflowDefinitionsApi.GetManyByIdAsync))
            {
                StoredReadWasBound = IsRequestContextBound?.Invoke() == true;
                StoredReadBindingStates.Add(StoredReadWasBound);
                var ids = (ICollection<string>)args![0]!;
                VersionIdReads.AddRange(ids);
                if (StoredReadException != null)
                    return Task.FromException<ListResponse<WorkflowDefinition>>(StoredReadException);

                var definitions = StoredDefinitions.Where(x => ids.Contains(x.Id)).ToList();
                return Task.FromResult(new ListResponse<WorkflowDefinition>(definitions, definitions.Count));
            }

            if (targetMethod?.Name == nameof(IWorkflowDefinitionsApi.ImportAsync))
            {
                ImportCalls++;
                var model = (WorkflowDefinitionModel)args![0]!;
                ImportedRoot = model.Root?.DeepClone();
                ImportedName = model.Name;
                ImportWasBound = IsRequestContextBound?.Invoke() == true;
                return Task.FromResult(new WorkflowDefinition());
            }

            if (targetMethod?.Name == nameof(IWorkflowDefinitionsApi.PublishAsync))
            {
                PublishCalls++;
                PublishWasBound = IsRequestContextBound?.Invoke() == true;
                return Task.FromResult(new SaveWorkflowDefinitionResponse(new WorkflowDefinition(), false, 0));
            }

            if (targetMethod?.Name == nameof(IWorkflowDefinitionsApi.BulkPublishAsync))
            {
                BulkPublishCalls++;
                BulkPublishRequestIds.AddRange(((BulkPublishWorkflowDefinitionsRequest)args![0]!).DefinitionIds);
                BulkPublishWasBound = IsRequestContextBound?.Invoke() == true;
                return Task.FromResult(BulkPublishResponse ?? new BulkPublishWorkflowDefinitionsResponse([], [], [], []));
            }

            if (targetMethod?.Name != nameof(IWorkflowDefinitionsApi.SaveAsync))
                throw new NotSupportedException(targetMethod?.Name);

            SaveCalls++;
            var request = (SaveWorkflowDefinitionRequest)args![0]!;
            SavedRoot = request.Model.Root?.DeepClone();
            SavedName = request.Model.Name;
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
