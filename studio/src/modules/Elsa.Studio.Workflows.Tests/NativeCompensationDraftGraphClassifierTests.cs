using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Providers;
using Elsa.Studio.Workflows.Domain.Services;
using Elsa.Studio.Workflows.Resolvers;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class NativeCompensationDraftGraphClassifierTests
{
    [Fact]
    public async Task ScopeLikeLiteralMetadataAndUnwrappedDeclaredInputRemainOrdinaryAndUnchanged()
    {
        var root = Support.NativeCompensationDraftTestData.CreateOrdinarySetVariableWithScopeLikeUserData();
        var originalJson = root.ToJsonString();
        var classifier = CreateClassifier(Support.NativeCompensationDraftTestData.CreateActivityRegistry());

        var analysis = await classifier.AnalyzeAsync(root);

        Assert.True(analysis.IsComplete);
        Assert.False(analysis.ContainsCompensationScope);
        Assert.False(analysis.RequiresNativeProtection);
        Assert.Equal(originalJson, root.ToJsonString());
    }

    [Fact]
    public async Task LegacyFallbackKeepsOrdinaryActivityDataOrdinary()
    {
        var analysis = await new NativeCompensationDraftGraphClassifier()
            .AnalyzeAsync(Support.NativeCompensationDraftTestData.CreateLegacySetVariableWithScopeLikeLiteralAndMetadata());

        Assert.True(analysis.IsComplete);
        Assert.False(analysis.ContainsCompensationScope);
        Assert.False(analysis.RequiresNativeProtection);
    }

    [Theory]
    [InlineData("""{"id":"sequence-1","type":"Elsa.Sequence","version":1,"activities":[{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}] }""")]
    [InlineData("""{"id":"flowchart-1","type":"Elsa.Flowchart","version":1,"activities":{"scope-1":{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}} }""")]
    [InlineData("""{"id":"if-1","type":"Elsa.If","version":1,"then":{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]} }""")]
    public async Task LegacyFallbackFailsClosedForScopeInsideBuiltInOwnedContainer(string json)
    {
        var analysis = await new NativeCompensationDraftGraphClassifier().AnalyzeAsync(JsonNode.Parse(json)!.AsObject());

        Assert.False(analysis.IsComplete);
        Assert.True(analysis.MustRejectProtectedWrite);
    }

    [Fact]
    public async Task SyntheticWorkflowWrapperExposesItsRootAsAnOwnedActivityEdge()
    {
        var root = JsonNode.Parse("""
            {"id":"workflow-1","type":"Elsa.Workflow","version":1,"root":{"type":"Elsa.CompensationScope","draftSchemaVersion":1,"steps":[]}}
            """)!.AsObject();

        var analysis = await CreateClassifier(CreateRegistry()).AnalyzeAsync(root);

        Assert.True(analysis.IsComplete);
        Assert.True(analysis.ContainsCompensationScope);
        Assert.True(analysis.ContainsUnsupportedScope);
    }

    [Fact]
    public async Task MalformedScopeInActualIfThenPortIsDetectedBeforeItsPinsAreValidated()
    {
        var ifDescriptor = new ActivityDescriptor
        {
            TypeName = "Elsa.If",
            Version = 1,
            Ports = [new() { Name = "Then", Type = PortType.Embedded }]
        };
        var registry = CreateRegistry(ifDescriptor);
        var root = JsonNode.Parse("""
            {"id":"if-1","type":"Elsa.If","version":1,"then":{"type":"Elsa.CompensationScope","draftSchemaVersion":1,"steps":[]}}
            """)!.AsObject();

        var analysis = await CreateClassifier(registry).AnalyzeAsync(root);

        Assert.True(analysis.IsComplete);
        Assert.True(analysis.ContainsCompensationScope);
        Assert.True(analysis.ContainsUnsupportedScope);
    }

    [Fact]
    public async Task DynamicPortProviderOwnedEdgeIsClassifiedEvenWhenDescriptorHasNoPorts()
    {
        var descriptor = new ActivityDescriptor { TypeName = "Test.DynamicContainer", Version = 1 };
        var root = JsonNode.Parse("""
            {"id":"container-1","type":"Test.DynamicContainer","version":1,"channel":{"current":{"type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}}}
            """)!.AsObject();
        var classifier = CreateClassifier(CreateRegistry(descriptor), new DynamicTestPortProvider("channel"));

        var analysis = await classifier.AnalyzeAsync(root);

        Assert.True(analysis.IsComplete);
        Assert.True(analysis.ContainsCompensationScope);
        Assert.True(analysis.ContainsUnsupportedScope);
    }

    [Fact]
    public async Task DynamicPortNamedMetadataClassifiesItsCurrentScopeChild()
    {
        var descriptor = new ActivityDescriptor { TypeName = "Test.DynamicContainer", Version = 1 };
        var root = JsonNode.Parse("""
            {"id":"container-1","type":"Test.DynamicContainer","version":1,"metadata":{"current":{"type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}}}
            """)!.AsObject();
        var classifier = CreateClassifier(CreateRegistry(descriptor), new DynamicTestPortProvider("metadata"));

        var analysis = await classifier.AnalyzeAsync(root);

        Assert.True(analysis.IsComplete);
        Assert.True(analysis.ContainsCompensationScope);
        Assert.True(analysis.ContainsUnsupportedScope);
    }

    [Theory]
    [InlineData("""{"id":"root-1","type":"Elsa.Sequence","version":1,"activities":[{"type":"Elsa.CompensationScope","draftSchemaVersion":1,"steps":[]}] }""")]
    [InlineData("""{"id":"root-1","type":"Elsa.Flowchart","version":1,"activities":{"scope-1":{"type":"Elsa.CompensationScope","draftSchemaVersion":1,"steps":[]}} }""")]
    [InlineData("""{"id":"root-1","type":"Elsa.StateMachine","version":1,"states":[{"name":"Ready","entry":{"type":"Elsa.CompensationScope","draftSchemaVersion":1,"steps":[]}}]}""")]
    [InlineData("""{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[{"stepId":"step-1","forward":{"type":"Elsa.CompensationScope","draftSchemaVersion":1,"steps":[]}}]}""")]
    public async Task NativeOwnedCollectionsExposeRawScopeMarkersBeforeActivityShapeFiltering(string json)
    {
        var analysis = await CreateClassifier(CreateRegistry()).AnalyzeAsync(JsonNode.Parse(json)!.AsObject());

        Assert.True(analysis.IsComplete);
        Assert.True(analysis.ContainsCompensationScope);
        Assert.True(analysis.ContainsUnsupportedScope);
    }

    [Fact]
    public async Task ExactScopePinsAndDeclaredChildInputsAreSeparated()
    {
        var root = JsonNode.Parse("""
            {"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[{"stepId":"step-1","forward":{"id":"write-1","type":"Elsa.WriteLine","version":1,"text":{"type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}}}]}
            """)!.AsObject();

        var analysis = await CreateClassifier(CreateRegistry()).AnalyzeAsync(root);

        Assert.True(analysis.IsComplete);
        Assert.True(analysis.ContainsCompensationScope);
        Assert.False(analysis.ContainsUnsupportedScope);
    }

    [Fact]
    public async Task NestedOpaqueNotFoundOriginalAtAnOwnedEdgeIsUnsupportedNativeEvidence()
    {
        var root = JsonNode.Parse("""
            {"id":"root-1","type":"Elsa.Sequence","version":1,"activities":[{"id":"missing-1","type":"Elsa.NotFoundActivity","missingTypeName":"Test.Missing","originalActivityJson":"{\"type\":\"Elsa.NotFoundActivity\",\"missingTypeName\":\"Test.MissingNested\",\"originalActivityJson\":\"{\\\"type\\\":\\\"Elsa.CompensationScope\\\",\\\"version\\\":1}\"}"}]}
            """)!.AsObject();

        var analysis = await CreateClassifier(CreateRegistry()).AnalyzeAsync(root);

        Assert.True(analysis.IsComplete);
        Assert.True(analysis.ContainsCompensationScope);
        Assert.True(analysis.ContainsUnsupportedScope);
    }

    [Fact]
    public async Task UnknownParentMetadataWithScopeMarkerIsIndeterminateAndImportDoesNotPost()
    {
        var registry = CreateRegistry();
        var root = JsonNode.Parse("""
            {"id":"custom-1","type":"Test.UnavailableParent","version":99,"nested":{"type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}}
            """)!.AsObject();
        var backend = new TestBackendApiClientProvider();
        var classifier = CreateClassifier(registry);
        var analysis = await classifier.AnalyzeAsync(root);
        var importer = new WorkflowDefinitionImporter(backend, CreateMediator(), new AnyWorkflowJsonDetector(), new EnabledFeatureProvider(), classifier);

        var result = await importer.ImportJsonAsync("workflow.json", SerializeWorkflowModel(root), null);

        Assert.False(analysis.IsComplete);
        Assert.True(analysis.HasPotentialScopeMarker);
        Assert.True(analysis.MustRejectProtectedWrite);
        Assert.NotNull(result.Failure);
        Assert.Equal(0, backend.ApiRequests);
    }

    [Fact]
    public async Task BackendContextChangeDuringRegistryLoadMakesClassificationIndeterminate()
    {
        var registry = new DelayedTestActivityRegistry(new ActivityDescriptor
        {
            TypeName = "Elsa.If",
            Version = 1,
            Ports = [new() { Name = "Then", Type = PortType.Embedded }]
        });
        var backend = new TestBackendApiClientProvider { ContextKey = "context-A" };
        var classifier = new NativeCompensationDraftGraphClassifier(
            registry,
            new DefaultActivityPortService([new DefaultActivityPortProvider()]),
            [new StateMachineActivityResolver(), new CompensationScopeActivityResolver(), new DefaultActivityResolver()],
            backend);
        var root = JsonNode.Parse("""
            {"id":"if-1","type":"Elsa.If","version":1,"then":{"type":"Elsa.CompensationScope","draftSchemaVersion":1,"steps":[]}}
            """)!.AsObject();

        var analysisTask = classifier.AnalyzeAsync(root);
        await registry.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        backend.ContextKey = "context-B";
        registry.Complete();

        var analysis = await analysisTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(analysis.IsComplete);
        Assert.True(analysis.MustRejectProtectedWrite);
    }

    [Fact]
    public async Task MalformedScopeInIfThenImportDoesNotPost()
    {
        var registry = CreateRegistry(new ActivityDescriptor
        {
            TypeName = "Elsa.If",
            Version = 1,
            Ports = [new() { Name = "Then", Type = PortType.Embedded }]
        });
        var backend = new TestBackendApiClientProvider();
        var root = JsonNode.Parse("""
            {"id":"if-1","type":"Elsa.If","version":1,"then":{"type":"Elsa.CompensationScope","draftSchemaVersion":1,"steps":[]}}
            """)!.AsObject();
        var importer = new WorkflowDefinitionImporter(backend, CreateMediator(), new AnyWorkflowJsonDetector(), new EnabledFeatureProvider(), CreateClassifier(registry));

        var result = await importer.ImportJsonAsync("workflow.json", SerializeWorkflowModel(root), null);

        Assert.NotNull(result.Failure);
        Assert.Equal(0, backend.ApiRequests);
    }

    [Fact]
    public async Task RegisteredResolverOwnedChildIsQualifiedAndItsWrapperIsNotGenericData()
    {
        var descriptor = new ActivityDescriptor { TypeName = "Test.CustomContainer", Version = 1 };
        var root = JsonNode.Parse("""
            {"id":"container-1","type":"Test.CustomContainer","version":1,"extensionData":{"current":{"type":"Elsa.CompensationScope","draftSchemaVersion":1,"steps":[]}}}
            """)!.AsObject();
        var classifier = CreateClassifier(CreateRegistry(descriptor), [], [new ReservedDataContainerResolver(), new DefaultActivityResolver()]);

        var analysis = await classifier.AnalyzeAsync(root);

        Assert.True(analysis.IsComplete);
        Assert.True(analysis.ContainsCompensationScope);
        Assert.True(analysis.ContainsUnsupportedScope);
    }

    [Fact]
    public async Task LegacyFallbackFailsClosedForUnknownActivityMarkerInsideReservedData()
    {
        var root = JsonNode.Parse("""
            {"id":"custom-1","type":"Test.UnknownContainer","version":1,"metadata":{"current":{"type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}}}
            """)!.AsObject();

        var analysis = await new NativeCompensationDraftGraphClassifier().AnalyzeAsync(root);

        Assert.False(analysis.IsComplete);
        Assert.True(analysis.MustRejectProtectedWrite);
    }

    private static NativeCompensationDraftGraphClassifier CreateClassifier(
        IActivityRegistry registry,
        params IActivityPortProvider[] providers) =>
        CreateClassifier(registry, providers, [new StateMachineActivityResolver(), new CompensationScopeActivityResolver(), new DefaultActivityResolver()]);

    private static NativeCompensationDraftGraphClassifier CreateClassifier(
        IActivityRegistry registry,
        IActivityPortProvider[] providers,
        IEnumerable<IActivityResolver> resolvers) =>
        new(registry, new DefaultActivityPortService([new DefaultActivityPortProvider(), ..providers]), resolvers, new TestBackendApiClientProvider());

    private static NativeCompensationDraftGraphClassifier CreateClassifier(
        IActivityRegistry registry,
        IActivityPortProvider provider) =>
        CreateClassifier(registry, [provider]);

    private static TestActivityRegistry CreateRegistry(params ActivityDescriptor[] extraDescriptors) =>
        new(
        [
            new ActivityDescriptor { TypeName = "Elsa.Sequence", Version = 1 },
            new ActivityDescriptor { TypeName = "Elsa.Flowchart", Version = 1 },
            new ActivityDescriptor { TypeName = "Elsa.StateMachine", Version = 1 },
            new ActivityDescriptor { TypeName = "Elsa.WriteLine", Version = 1, Inputs = [new() { Name = "text" }] },
            ..extraDescriptors
        ]);

    private static IMediator CreateMediator() => DispatchProxy.Create<IMediator, CompletedMediator>();

    private static string SerializeWorkflowModel(JsonObject root) =>
        JsonSerializer.Serialize(new WorkflowDefinitionModel { Root = root }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private sealed class TestActivityRegistry(IReadOnlyCollection<ActivityDescriptor> descriptors) : IActivityRegistry
    {
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<ActivityDescriptor> List() => descriptors;
        public ActivityDescriptor? Find(string activityType, int? version = default) =>
            version is { } exactVersion ? descriptors.FirstOrDefault(x => x.TypeName == activityType && x.Version == exactVersion) : null;
        public IEnumerable<ActivityDescriptor> FindAll(string activityType) => descriptors.Where(x => x.TypeName == activityType);
        public void MarkStale() { }
    }

    private sealed class DelayedTestActivityRegistry(params ActivityDescriptor[] descriptors) : IActivityRegistry
    {
        private readonly TaskCompletionSource _continue = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Complete() => _continue.TrySetResult();
        public Task RefreshAsync(CancellationToken cancellationToken = default) => EnsureLoadedAsync(cancellationToken);
        public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            await _continue.Task.WaitAsync(cancellationToken);
        }

        public IEnumerable<ActivityDescriptor> List() => descriptors;
        public ActivityDescriptor? Find(string activityType, int? version = default) =>
            version is { } exactVersion ? descriptors.FirstOrDefault(x => x.TypeName == activityType && x.Version == exactVersion) : null;
        public IEnumerable<ActivityDescriptor> FindAll(string activityType) => descriptors.Where(x => x.TypeName == activityType);
        public void MarkStale() { }
    }

    private sealed class DynamicTestPortProvider(string portName) : ActivityPortProviderBase
    {
        public override double Priority => 100;
        public override bool GetSupportsActivityType(PortProviderContext context) => context.ActivityDescriptor.TypeName == "Test.DynamicContainer";
        public override IEnumerable<Port> GetPorts(PortProviderContext context) => [new() { Name = portName, Type = PortType.Embedded }];
        public override JsonObject? ResolvePort(string resolvedPortName, PortProviderContext context) =>
            resolvedPortName == portName ? context.Activity[portName]?["current"] as JsonObject : null;
    }

    private sealed class ReservedDataContainerResolver : IActivityResolver
    {
        public int Priority => 100;
        public bool GetSupportsActivity(JsonObject activity) => activity["type"]?.GetValue<string>() == "Test.CustomContainer";
        public ValueTask<IEnumerable<EmbeddedActivity>> GetActivitiesAsync(JsonObject activity, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IEnumerable<EmbeddedActivity>>(
            [new(activity["extensionData"]!["current"]!.AsObject(), "extensionData")]);
    }

    private sealed class AnyWorkflowJsonDetector : IWorkflowJsonDetector
    {
        public bool IsWorkflowSchema(string json) => true;
    }

    private sealed class EnabledFeatureProvider : IRemoteFeatureProvider, IRemoteFeatureProviderWithFreshCheck
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>>([]);
        public Task<bool> IsEnabledFreshAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class TestBackendApiClientProvider : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://backend.example/");
        public string ContextKey { get; set; } = "context-A";
        public int ApiRequests { get; private set; }

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class
        {
            ApiRequests++;
            throw new InvalidOperationException("The importer should refuse before obtaining a write API.");
        }
    }

    private class CompletedMediator : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.ReturnType == typeof(Task))
                return Task.CompletedTask;
            throw new NotSupportedException($"The test mediator does not support {targetMethod?.Name}.");
        }
    }
}
