using System.Diagnostics.CodeAnalysis;
using AngleSharp.Dom;
using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.DiagramDesigners.CompensationScopes;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class CompensationScopeDesignerCapabilityLifecycleTests : BunitContext, IAsyncLifetime
{
    private const string BackendContextKey = "context-A";
    private readonly TestBackendApiClientProvider _backend = new(BackendContextKey);
    private readonly TestActivityRegistry _activityRegistry = new();
    private TestFreshFeatureProvider? _featureProvider;

    public CompensationScopeDesignerCapabilityLifecycleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddMudServices();
        Services.AddSingleton<IActivityRegistry>(_activityRegistry);
        Services.AddSingleton<IBackendApiClientProvider>(_backend);
        Services.AddSingleton<IRemoteFeatureProvider>(_ => _featureProvider!);
        Services.AddSingleton<IIdentityGenerator, TestIdentityGenerator>();
        Services.AddSingleton<IActivityNameGenerator, TestActivityNameGenerator>();
        Render<MudPopoverProvider>();
    }

    [Fact]
    public async Task SameContextRevalidationKeepsTheEditorMountedAndNegativeResultMakesItReadOnly()
    {
        var pendingCheck = NewCompletionSource<bool>();
        var secondCheckStarted = NewCompletionSource();
        _featureProvider = new TestFreshFeatureProvider((count, _) => count switch
        {
            1 => Task.FromResult(true),
            2 => SignalAndReturn(secondCheckStarted, pendingCheck.Task),
            _ => Task.FromResult(true)
        });
        var activity = CreateScope();
        var cut = RenderScope(activity, BackendContextKey);
        cut.WaitForAssertion(() => Assert.NotNull(FindStepNameInput(cut)));
        var originalInputId = FindStepNameInputId(cut);

        var revalidation = SetParametersAsync(cut, activity, BackendContextKey);
        await secondCheckStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var inputDuringRevalidation = FindStepNameInput(cut);
        Assert.Equal(originalInputId, FindStepNameInputId(cut));
        inputDuringRevalidation.Change("Typed during capability refresh");
        Assert.Equal("Typed during capability refresh", activity["steps"]![0]!["name"]!.GetValue<string>());
        Assert.Equal(originalInputId, FindStepNameInputId(cut));

        pendingCheck.SetResult(false);
        await revalidation;
        Assert.DoesNotContain(cut.FindAll("label"), label => label.TextContent.Trim() == "Step name");
        Assert.Contains("cannot verify safe native compensation draft authoring", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SameContextPositiveRevalidationKeepsTheSameStepInputMounted()
    {
        var pendingCheck = NewCompletionSource<bool>();
        var secondCheckStarted = NewCompletionSource();
        _featureProvider = new TestFreshFeatureProvider((count, _) => count switch
        {
            1 => Task.FromResult(true),
            2 => SignalAndReturn(secondCheckStarted, pendingCheck.Task),
            _ => Task.FromResult(true)
        });
        var activity = CreateScope();
        var cut = RenderScope(activity, BackendContextKey);
        cut.WaitForAssertion(() => Assert.NotNull(FindStepNameInput(cut)));
        var originalInputId = FindStepNameInputId(cut);

        var revalidation = SetParametersAsync(cut, activity, BackendContextKey);
        await secondCheckStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(originalInputId, FindStepNameInputId(cut));
        FindStepNameInput(cut).Change("Typed while checking");
        Assert.Equal("Typed while checking", activity["steps"]![0]!["name"]!.GetValue<string>());

        pendingCheck.SetResult(true);
        await revalidation;
        Assert.Equal(originalInputId, FindStepNameInputId(cut));
        Assert.Equal("Typed while checking", activity["steps"]![0]!["name"]!.GetValue<string>());
        Assert.DoesNotContain("cannot verify safe native compensation draft authoring", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SupersededRegistryLoadCannotStartAnObsoleteFeatureCheck()
    {
        var obsoleteRegistryLoad = NewCompletionSource();
        var obsoleteRegistryLoadStarted = NewCompletionSource();
        _activityRegistry.EnsureLoaded = (count, _) => count switch
        {
            2 => SignalAndWait(obsoleteRegistryLoadStarted, obsoleteRegistryLoad.Task),
            _ => Task.CompletedTask
        };
        _featureProvider = new TestFreshFeatureProvider((_, _) => Task.FromResult(true));
        var activity = CreateScope();
        var cut = RenderScope(activity, BackendContextKey);
        cut.WaitForAssertion(() => Assert.NotNull(FindStepNameInput(cut)));

        var obsoleteCheck = SetParametersAsync(cut, activity, BackendContextKey);
        await obsoleteRegistryLoadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var currentCheck = SetParametersAsync(cut, activity, BackendContextKey);
        await currentCheck;
        Assert.Equal(2, _featureProvider.FreshCheckCount);

        obsoleteRegistryLoad.SetResult();
        await obsoleteCheck;
        Assert.Equal(2, _featureProvider.FreshCheckCount);
        Assert.NotNull(FindStepNameInput(cut));
    }

    [Fact]
    public async Task ContextSwitchClearsTheEditorAndAnOldPositiveResultCannotRestoreIt()
    {
        var pendingCheck = NewCompletionSource<bool>();
        var secondCheckStarted = NewCompletionSource();
        _featureProvider = new TestFreshFeatureProvider((count, _) => count switch
        {
            1 => Task.FromResult(true),
            2 => SignalAndReturn(secondCheckStarted, pendingCheck.Task),
            _ => Task.FromResult(true)
        });
        var activity = CreateScope();
        var cut = RenderScope(activity, BackendContextKey);
        cut.WaitForAssertion(() => Assert.NotNull(FindStepNameInput(cut)));

        var obsoleteCheck = SetParametersAsync(cut, activity, BackendContextKey);
        await secondCheckStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        _backend.ContextKey = "context-B";
        await SetParametersAsync(cut, activity, BackendContextKey);
        Assert.DoesNotContain(cut.FindAll("label"), label => label.TextContent.Trim() == "Step name");

        pendingCheck.SetResult(true);
        await obsoleteCheck;
        Assert.DoesNotContain(cut.FindAll("label"), label => label.TextContent.Trim() == "Step name");
        Assert.Contains("cannot verify safe native compensation draft authoring", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, _featureProvider.FreshCheckCount);
    }

    [Fact]
    public async Task FailedSameContextRecheckRevokesThePreviouslyVerifiedEditor()
    {
        _featureProvider = new TestFreshFeatureProvider((count, _) => count switch
        {
            1 => Task.FromResult(true),
            _ => Task.FromException<bool>(new InvalidOperationException("Feature check failed."))
        });
        var activity = CreateScope();
        var cut = RenderScope(activity, BackendContextKey);
        cut.WaitForAssertion(() => Assert.NotNull(FindStepNameInput(cut)));

        await SetParametersAsync(cut, activity, BackendContextKey);

        Assert.DoesNotContain(cut.FindAll("label"), label => label.TextContent.Trim() == "Step name");
        Assert.Contains("cannot verify safe native compensation draft authoring", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, _featureProvider.FreshCheckCount);
    }

    [Theory]
    [InlineData("read-only")]
    [InlineData("unsupported-shape")]
    [InlineData("unsupported-child-pin")]
    [InlineData("unbound-request-context")]
    [InlineData("changed-document-context")]
    public async Task UnsafeScopeParameterChangesClearEditableControlsBeforeARefreshCanComplete(string change)
    {
        var pendingCheck = NewCompletionSource<bool>();
        var secondCheckStarted = NewCompletionSource();
        _featureProvider = new TestFreshFeatureProvider((count, _) => count switch
        {
            1 => Task.FromResult(true),
            2 => SignalAndReturn(secondCheckStarted, pendingCheck.Task),
            _ => Task.FromResult(true)
        });
        var activity = CreateScope();
        var documentContextKey = BackendContextKey;
        var isReadOnly = false;
        var cut = RenderScope(activity, documentContextKey);
        cut.WaitForAssertion(() => Assert.NotNull(FindStepNameInput(cut)));

        switch (change)
        {
            case "read-only":
                isReadOnly = true;
                break;
            case "unsupported-shape":
                activity["draftSchemaVersion"] = 2;
                break;
            case "unsupported-child-pin":
                activity["steps"]![0]!["forward"] = System.Text.Json.Nodes.JsonNode.Parse("""
                    {"id":"missing-1","nodeId":"scope-1:missing-1","type":"Elsa.MissingActivity","version":1}
                    """);
                break;
            case "unbound-request-context":
                _backend.SupportsAuthenticatedRequestContextBinding = false;
                break;
            case "changed-document-context":
                documentContextKey = "context-B";
                break;
        }

        var parameterUpdate = SetParametersAsync(cut, activity, documentContextKey, isReadOnly);

        Assert.DoesNotContain(cut.FindAll("label"), label => label.TextContent.Trim() == "Step name");
        if (change == "read-only")
        {
            await secondCheckStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            pendingCheck.SetResult(true);
        }

        await parameterUpdate;
        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain(cut.FindAll("label"), label => label.TextContent.Trim() == "Step name");
            Assert.Contains(change == "unsupported-shape" || change == "unsupported-child-pin"
                ? "preserved as opaque, read-only data"
                : "cannot verify safe native compensation draft authoring", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Equal(change == "read-only" ? 2 : 1, _featureProvider.FreshCheckCount);
    }

    [Fact]
    public void InitialNegativeFeatureCheckNeverShowsEditableControls()
    {
        _featureProvider = new TestFreshFeatureProvider((_, _) => Task.FromResult(false));
        var cut = RenderScope(CreateScope(), BackendContextKey);

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain(cut.FindAll("label"), label => label.TextContent.Trim() == "Step name");
            Assert.Contains("cannot verify safe native compensation draft authoring", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Equal(1, _featureProvider.FreshCheckCount);
    }

    [Fact]
    public void InitialUnsupportedShapeIsOpaqueWithoutAFeatureCheck()
    {
        _featureProvider = new TestFreshFeatureProvider((_, _) => Task.FromResult(true));
        var activity = CreateScope();
        activity["draftSchemaVersion"] = 2;

        var cut = RenderScope(activity, BackendContextKey);

        Assert.DoesNotContain(cut.FindAll("label"), label => label.TextContent.Trim() == "Step name");
        Assert.Contains("preserved as opaque, read-only data", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, _featureProvider.FreshCheckCount);
    }

    [Fact]
    public void LiveAuthBindingRevocationBlocksEditsBeforeTheNextRender()
    {
        _featureProvider = new TestFreshFeatureProvider((_, _) => Task.FromResult(true));
        var activity = CreateScope();
        var cut = RenderScope(activity, BackendContextKey);
        cut.WaitForAssertion(() => Assert.NotNull(FindStepNameInput(cut)));

        _backend.SupportsAuthenticatedRequestContextBinding = false;
        cut.Find("button[aria-label='Add compensation step']").Click();

        Assert.Single((System.Text.Json.Nodes.JsonArray)activity["steps"]!);
        cut.Render(parameters => parameters
            .Add(x => x.Activity, activity)
            .Add(x => x.DocumentBackendContextKey, BackendContextKey));
        Assert.DoesNotContain(cut.FindAll("label"), label => label.TextContent.Trim() == "Step name");
        Assert.Equal(1, _featureProvider.FreshCheckCount);
    }

    private IRenderedComponent<CompensationScopeDesigner> RenderScope(System.Text.Json.Nodes.JsonObject activity, string documentContextKey, bool isReadOnly = false) =>
        Render<CompensationScopeDesigner>(parameters => parameters
            .Add(x => x.Activity, activity)
            .Add(x => x.DocumentBackendContextKey, documentContextKey)
            .Add(x => x.IsReadOnly, isReadOnly));

    private static Task SetParametersAsync(IRenderedComponent<CompensationScopeDesigner> cut, System.Text.Json.Nodes.JsonObject activity, string documentContextKey, bool isReadOnly = false)
    {
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(CompensationScopeDesigner.Activity)] = activity,
            [nameof(CompensationScopeDesigner.DocumentBackendContextKey)] = documentContextKey,
            [nameof(CompensationScopeDesigner.IsReadOnly)] = isReadOnly
        });
        return cut.InvokeAsync(() => cut.Instance.SetParametersAsync(parameters));
    }

    private static IElement FindStepNameInput(IRenderedComponent<CompensationScopeDesigner> cut)
    {
        var label = cut.FindAll("label").Single(element => element.TextContent.Trim() == "Step name");
        return cut.Find($"#{label.GetAttribute("for")}");
    }

    private static string? FindStepNameInputId(IRenderedComponent<CompensationScopeDesigner> cut) => FindStepNameInput(cut).GetAttribute("id");

    private static System.Text.Json.Nodes.JsonObject CreateScope() => System.Text.Json.Nodes.JsonNode.Parse("""
        {"id":"scope-1","nodeId":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[{"stepId":"step-1","name":"Initial step","forward":null,"handler":null,"mappings":[]}]}
        """)!.AsObject();

    private static async Task<T> SignalAndReturn<T>(TaskCompletionSource started, Task<T> result)
    {
        started.TrySetResult();
        return await result;
    }

    private static async Task SignalAndWait(TaskCompletionSource started, Task result)
    {
        started.TrySetResult();
        await result;
    }

    private static TaskCompletionSource<T> NewCompletionSource<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static TaskCompletionSource NewCompletionSource() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    private sealed class TestFreshFeatureProvider(Func<int, CancellationToken, Task<bool>> check) : IRemoteFeatureProvider, IRemoteFeatureProviderWithFreshCheck
    {
        public int FreshCheckCount { get; private set; }

        public Task<bool> IsEnabledFreshAsync(string featureName, CancellationToken cancellationToken = default) =>
            check(++FreshCheckCount, cancellationToken);

        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }

    private sealed class TestActivityRegistry : IActivityRegistry
    {
        private static readonly ActivityDescriptor WriteLine = new()
        {
            Name = "WriteLine",
            TypeName = "Elsa.WriteLine",
            Version = 1,
            Inputs = [new() { Name = "Text" }]
        };

        public Func<int, CancellationToken, Task>? EnsureLoaded { get; set; }
        public int EnsureLoadedCount { get; private set; }

        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) =>
            EnsureLoaded?.Invoke(++EnsureLoadedCount, cancellationToken) ?? Task.CompletedTask;
        public IEnumerable<ActivityDescriptor> List() => [WriteLine];
        public ActivityDescriptor? Find(string activityType, int? version = default) => activityType == WriteLine.TypeName && (version is null || version == WriteLine.Version) ? WriteLine : null;
        public IEnumerable<ActivityDescriptor> FindAll(string activityType) => activityType == WriteLine.TypeName ? [WriteLine] : [];
        public void MarkStale() { }
    }

    private sealed class TestIdentityGenerator : IIdentityGenerator
    {
        public string GenerateId() => "generated-id";
    }

    private sealed class TestActivityNameGenerator : IActivityNameGenerator
    {
        public bool GetNameExists(IEnumerable<System.Text.Json.Nodes.JsonObject> activities, string name) => false;
        public string GenerateNextName(IEnumerable<System.Text.Json.Nodes.JsonObject> activities, ActivityDescriptor activityDescriptor) => "WriteLine1";
    }

    private sealed class TestBackendApiClientProvider(string contextKey) : IBackendApiClientProvider, IBackendRequestContextBinding
    {
        public Uri Url { get; } = new("https://backend.example/");
        public string ContextKey { get; set; } = contextKey;
        public bool SupportsAuthenticatedRequestContextBinding { get; set; } = true;
        public bool HasActiveRequestContextBinding => false;

        public void RegisterAuthenticatedRequestTransport(IBackendRequestContextEpochSource? epochSource) { }
        public IDisposable BindRequestContext(string expectedContextKey) => throw new NotSupportedException();
        public void ValidateBoundRequestContext(IBackendRequestContextEpochSource? requestEpochSource) { }

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class =>
            throw new InvalidOperationException("The lifecycle test must not create a backend client.");
    }
}
