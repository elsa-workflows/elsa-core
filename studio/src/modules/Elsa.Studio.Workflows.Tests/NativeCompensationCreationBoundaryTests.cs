using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;
using Elsa.Studio.Workflows.DiagramDesigners.Sequences;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Interop;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Designer.Services;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class NativeCompensationCreationBoundaryTests : BunitContext, IAsyncLifetime
{
    private const string BackendContextKey = "context-A";

    public NativeCompensationCreationBoundaryTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task FlowchartDrop_AwaitsFreshCapabilityBeforeSelectingExactlyOneNativeActivity()
    {
        var featureProvider = new DeferredFeatureProvider();
        using var wrapper = CreateFlowchartWrapper(featureProvider, new MutableBackendProvider(BackendContextKey), BackendContextKey);
        var created = new List<JsonObject>();
        SetParameter(wrapper, nameof(FlowchartDesignerWrapper.ActivitySelected), EventCallback.Factory.Create<JsonObject>(created, created.Add));
        wrapper.GetType().GetProperty("Flowchart")!.SetValue(wrapper, CreateRoot("Elsa.Flowchart"));
        SetPrivateProperty(wrapper, "DragDropManager", new DragDropManager { Payload = NativeDescriptor });

        var drop = InvokePrivateAsync(wrapper, "OnDrop", new Microsoft.AspNetCore.Components.Web.DragEventArgs { PageX = 12, PageY = 34 });
        Assert.Same(featureProvider.Started.Task, await Task.WhenAny(featureProvider.Started.Task, drop));

        Assert.Empty(created);
        featureProvider.Complete(true);
        await drop;

        var activity = Assert.Single(created);
        Assert.Equal(NativeCompensationDraftCapability.CompensationScopeActivityTypeName, activity["type"]!.GetValue<string>());
        Assert.Equal(BackendContextKey, GetParameter<string>(wrapper, nameof(FlowchartDesignerWrapper.DocumentBackendContextKey)));
    }

    [Fact]
    public async Task SequenceDrop_DoesNotCreateAfterTheBackendChangesDuringFreshCheck()
    {
        var featureProvider = new DeferredFeatureProvider();
        var backendProvider = new MutableBackendProvider(BackendContextKey);
        using var wrapper = CreateSequenceWrapper(featureProvider, backendProvider, BackendContextKey);
        var created = new List<JsonObject>();
        SetParameter(wrapper, nameof(SequenceDesignerWrapper.ActivitySelected), EventCallback.Factory.Create<JsonObject>(created, created.Add));
        wrapper.GetType().GetProperty("Sequence")!.SetValue(wrapper, CreateRoot("Elsa.Sequence"));
        SetPrivateProperty(wrapper, "DragDropManager", new DragDropManager { Payload = NativeDescriptor });

        var drop = InvokePrivateAsync(wrapper, "OnDrop", new Microsoft.AspNetCore.Components.Web.DragEventArgs { PageX = 12, PageY = 34 });
        Assert.Same(featureProvider.Started.Task, await Task.WhenAny(featureProvider.Started.Task, drop));
        backendProvider.ContextKey = "context-B";
        featureProvider.Complete(true);
        await drop;

        Assert.Empty(created);
    }

    [Fact]
    public async Task SequenceDrop_OrdinaryActivityDoesNotReadCapabilityOrBackendContext()
    {
        var featureProvider = new DeferredFeatureProvider();
        var backendProvider = new MutableBackendProvider(BackendContextKey) { ThrowOnContextRead = true };
        using var wrapper = CreateSequenceWrapper(featureProvider, backendProvider, "");
        var created = new List<JsonObject>();
        SetParameter(wrapper, nameof(SequenceDesignerWrapper.ActivitySelected), EventCallback.Factory.Create<JsonObject>(created, created.Add));
        wrapper.GetType().GetProperty("Sequence")!.SetValue(wrapper, CreateRoot("Elsa.Sequence"));
        SetPrivateProperty(wrapper, "DragDropManager", new DragDropManager { Payload = new ActivityDescriptor { TypeName = "Elsa.WriteLine", Version = 1 } });

        await InvokePrivateAsync(wrapper, "OnDrop", new Microsoft.AspNetCore.Components.Web.DragEventArgs { PageX = 12, PageY = 34 });

        Assert.Equal("Elsa.WriteLine", Assert.Single(created)["type"]!.GetValue<string>());
        Assert.Equal(0, featureProvider.FreshCheckCount);
    }

    [Fact]
    public async Task StateMachineStateSlot_DoesNotMutateUntilFreshCapabilityAllowsIt()
    {
        var featureProvider = new DeferredFeatureProvider();
        var component = RenderStateMachine(featureProvider, new MutableBackendProvider(BackendContextKey), BackendContextKey);
        var wrapper = component.Instance;
        var session = GetPrivateField<StateMachineEditorSession>(wrapper, "_session");
        var stateId = session.ProjectCanvas().States.Single(x => x.Name == "Pending").VisualId;
        await component.InvokeAsync(() => SetPrivateField(wrapper, "_selectedStateId", stateId));

        var apply = component.InvokeAsync(() => InvokePrivateAsync(wrapper, "ApplyStateActivityDescriptorAsync", "entry", NativeDescriptor, false));
        Assert.Same(featureProvider.Started.Task, await Task.WhenAny(featureProvider.Started.Task, apply));
        Assert.Null(session.GetState(stateId).Entry);

        featureProvider.Complete(true);
        await apply;
        var created = Assert.IsType<JsonObject>(session.GetState(stateId).Entry);
        Assert.Equal(NativeCompensationDraftCapability.CompensationScopeActivityTypeName, created["type"]!.GetValue<string>());
    }

    [Fact]
    public async Task StateMachineTransitionSlot_DoesNotMutateWhenDocumentOriginIsMissing()
    {
        var featureProvider = new DeferredFeatureProvider();
        var component = RenderStateMachine(featureProvider, new MutableBackendProvider(BackendContextKey), "");
        var wrapper = component.Instance;
        var session = GetPrivateField<StateMachineEditorSession>(wrapper, "_session");
        var transitionId = session.ProjectCanvas().Transitions.Single().VisualId;
        await component.InvokeAsync(() => SetPrivateField(wrapper, "_selectedTransitionId", transitionId));

        await component.InvokeAsync(() => InvokePrivateAsync(wrapper, "ApplyTransitionActivityDescriptorAsync", "action", NativeDescriptor, false));

        Assert.Null(session.GetTransition(transitionId).Action);
        Assert.Equal(0, featureProvider.FreshCheckCount);
    }

    [Fact]
    public async Task StateMachineTransitionSlot_DoesNotMutateWhenFreshCapabilityIsDenied()
    {
        var featureProvider = new DeferredFeatureProvider();
        var component = RenderStateMachine(featureProvider, new MutableBackendProvider(BackendContextKey), BackendContextKey);
        var wrapper = component.Instance;
        var session = GetPrivateField<StateMachineEditorSession>(wrapper, "_session");
        var transitionId = session.ProjectCanvas().Transitions.Single().VisualId;
        await component.InvokeAsync(() => SetPrivateField(wrapper, "_selectedTransitionId", transitionId));

        var apply = component.InvokeAsync(() => InvokePrivateAsync(wrapper, "ApplyTransitionActivityDescriptorAsync", "action", NativeDescriptor, false));
        await featureProvider.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        featureProvider.Complete(false);
        await apply;

        Assert.Null(session.GetTransition(transitionId).Action);
    }

    [Fact]
    public async Task StateMachineTransitionSlot_RejectsAStaleSelectionAfterFreshCheck()
    {
        var featureProvider = new DeferredFeatureProvider();
        var component = RenderStateMachine(featureProvider, new MutableBackendProvider(BackendContextKey), BackendContextKey);
        var wrapper = component.Instance;
        var session = GetPrivateField<StateMachineEditorSession>(wrapper, "_session");
        var transitionId = session.ProjectCanvas().Transitions.Single().VisualId;
        await component.InvokeAsync(() => SetPrivateField(wrapper, "_selectedTransitionId", transitionId));

        var apply = component.InvokeAsync(() => InvokePrivateAsync(wrapper, "ApplyTransitionActivityDescriptorAsync", "action", NativeDescriptor, false));
        await featureProvider.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await component.InvokeAsync(() => SetPrivateField(wrapper, "_selectedTransitionId", "another-transition"));
        featureProvider.Complete(true);
        await apply;

        Assert.Null(session.GetTransition(transitionId).Action);
    }

    private static ActivityDescriptor NativeDescriptor => new()
    {
        Name = "CompensationScope",
        TypeName = NativeCompensationDraftCapability.CompensationScopeActivityTypeName,
        Version = 1
    };

    private static FlowchartDesignerWrapper CreateFlowchartWrapper(
        DeferredFeatureProvider featureProvider,
        MutableBackendProvider backendProvider,
        string documentContextKey)
    {
        var wrapper = new FlowchartDesignerWrapper();
        SetPrivateProperty(wrapper, "RemoteFeatureProvider", featureProvider);
        SetPrivateProperty(wrapper, "BackendApiClientProvider", backendProvider);
        SetPrivateProperty(wrapper, "IdentityGenerator", new FixedIdentityGenerator("Scope1"));
        SetPrivateProperty(wrapper, "ActivityNameGenerator", new FixedActivityNameGenerator("Scope1"));
        SetParameter(wrapper, nameof(FlowchartDesignerWrapper.DocumentBackendContextKey), documentContextKey);
        return wrapper;
    }

    private static SequenceDesignerWrapper CreateSequenceWrapper(
        DeferredFeatureProvider featureProvider,
        MutableBackendProvider backendProvider,
        string documentContextKey)
    {
        var wrapper = new SequenceDesignerWrapper();
        SetPrivateProperty(wrapper, "RemoteFeatureProvider", featureProvider);
        SetPrivateProperty(wrapper, "BackendApiClientProvider", backendProvider);
        SetPrivateProperty(wrapper, "IdentityGenerator", new FixedIdentityGenerator("Scope1"));
        SetPrivateProperty(wrapper, "ActivityNameGenerator", new FixedActivityNameGenerator("Scope1"));
        SetParameter(wrapper, nameof(SequenceDesignerWrapper.DocumentBackendContextKey), documentContextKey);
        return wrapper;
    }

    private IRenderedComponent<StateMachineDesignerWrapper> RenderStateMachine(
        DeferredFeatureProvider featureProvider,
        MutableBackendProvider backendProvider,
        string documentContextKey)
    {
        var root = JsonNode.Parse("""
        {
          "id": "Machine1",
          "nodeId": "Workflow1:Machine1",
          "type": "Elsa.StateMachine",
          "states": [{ "name": "Pending" }, { "name": "Approved" }],
          "transitions": [{ "name": "Approve", "from": "Pending", "to": "Approved" }]
        }
        """)!.AsObject();

        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<StateMachineValidator>();
        Services.AddSingleton<IStateMachineMapper>(services => new StateMachineMapper(services.GetRequiredService<StateMachineValidator>()));
        Services.AddSingleton<IDiagramDesignerService, EmptyDiagramDesignerService>();
        Services.AddSingleton<IExpressionService, EmptyExpressionService>();
        Services.AddSingleton<IThemeService, TestThemeService>();
        Services.AddSingleton<IRemoteFeatureProvider>(featureProvider);
        Services.AddSingleton<IBackendApiClientProvider>(backendProvider);
        Services.AddSingleton<IIdentityGenerator>(new FixedIdentityGenerator("Scope1"));
        Services.AddSingleton<IActivityNameGenerator>(new FixedActivityNameGenerator("Scope1"));
        Services.AddScoped(services => new DesignerJsInterop(
            services.GetRequiredService<IJSRuntime>(),
            Microsoft.Extensions.Options.Options.Create(new DesignerOptions()),
            services,
            services.GetRequiredService<IThemeService>()));
        var module = JSInterop.SetupModule("./_content/Elsa.Studio.Workflows.Designer/designer.entry.js");
        module.Setup<string>("createGraph").SetResult("graph");

        return Render<StateMachineDesignerWrapper>(parameters => parameters
            .Add(x => x.StateMachine, root)
            .Add(x => x.DocumentBackendContextKey, documentContextKey));
    }

    private static JsonObject CreateRoot(string typeName) => new()
    {
        ["id"] = "Root1",
        ["nodeId"] = "Workflow1:Root1",
        ["type"] = typeName,
        ["version"] = 1,
        ["activities"] = new JsonArray()
    };

    private static Task InvokePrivateAsync(object target, string methodName, params object?[] arguments) =>
        (Task)target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, arguments)!;

    private static void SetParameter<T>(object component, string propertyName, T value) =>
        component.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!.SetValue(component, value);

    private static T GetParameter<T>(object component, string propertyName) =>
        (T)component.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!.GetValue(component)!;

    private static void SetPrivateProperty(object component, string propertyName, object value) =>
        component.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(component, value);

    private static void SetPrivateField(object component, string fieldName, object value) =>
        component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(component, value);

    private static T GetPrivateField<T>(object component, string fieldName) =>
        (T)component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component)!;

    private sealed class DeferredFeatureProvider : IRemoteFeatureProvider, IRemoteFeatureProviderWithFreshCheck
    {
        private readonly TaskCompletionSource<bool> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int FreshCheckCount { get; private set; }

        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<FeatureDescriptor>>([]);

        public Task<bool> IsEnabledFreshAsync(string featureName, CancellationToken cancellationToken = default)
        {
            FreshCheckCount++;
            Started.TrySetResult();
            return _result.Task;
        }

        public void Complete(bool enabled) => _result.TrySetResult(enabled);
    }

    private sealed class MutableBackendProvider(string contextKey) : IBackendApiClientProvider, IBackendRequestContextBinding
    {
        private string _contextKey = contextKey;
        public Uri Url { get; } = new("https://backend.example/");
        public string ContextKey
        {
            get => ThrowOnContextRead ? throw new InvalidOperationException("Ordinary activity creation must not inspect backend context.") : _contextKey;
            set => _contextKey = value;
        }
        public bool ThrowOnContextRead { get; set; }
        public bool SupportsAuthenticatedRequestContextBinding => true;
        public bool HasActiveRequestContextBinding => false;

        public void RegisterAuthenticatedRequestTransport(IBackendRequestContextEpochSource? epochSource) { }
        public IDisposable BindRequestContext(string expectedContextKey) => throw new NotSupportedException();
        public void ValidateBoundRequestContext(IBackendRequestContextEpochSource? requestEpochSource) { }
        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class =>
            throw new NotSupportedException();
    }

    private sealed class FixedIdentityGenerator(string id) : IIdentityGenerator
    {
        public string GenerateId() => id;
    }

    private sealed class FixedActivityNameGenerator(string name) : IActivityNameGenerator
    {
        public bool GetNameExists(IEnumerable<JsonObject> activities, string candidate) => activities.Any(x => x["name"]?.GetValue<string>() == candidate);
        public string GenerateNextName(IEnumerable<JsonObject> activities, ActivityDescriptor activityDescriptor) => name;
    }

    private sealed class EmptyDiagramDesignerService : IDiagramDesignerService
    {
        public bool HasDiagramDesigner(JsonObject activity) => false;
        public IDiagramDesigner GetDiagramDesigner(JsonObject activity) => throw new NotSupportedException();
    }

    private sealed class EmptyExpressionService : IExpressionService
    {
        public Task<IEnumerable<Elsa.Api.Client.Resources.Scripting.Models.ExpressionDescriptor>> ListDescriptorsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Elsa.Api.Client.Resources.Scripting.Models.ExpressionDescriptor>>([]);

        public Task<Elsa.Api.Client.Resources.Scripting.Models.ExpressionDescriptor?> GetByTypeAsync(string type, CancellationToken cancellationToken = default) =>
            Task.FromResult<Elsa.Api.Client.Resources.Scripting.Models.ExpressionDescriptor?>(null);
    }

    private sealed class TestThemeService : IThemeService
    {
        public event Action CurrentThemeChanged { add { } remove { } }
        public event Action IsDarkModeChanged { add { } remove { } }
        public MudTheme CurrentTheme { get; set; } = new();
        public bool IsDarkMode { get; set; }
    }
}
