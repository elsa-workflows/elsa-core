using System.Text.Json;
using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.OutputConverters.Models;
using Elsa.Api.Client.Resources.VariableTypes.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs.Outputs.Components;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs.Outputs.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests.OutputConverters;

public sealed class OutputsTabConverterTests : BunitContext, IAsyncLifetime
{
    private readonly OutputConverterServiceStub _converterService = new();
    private readonly JsonObject _activity = new()
    {
        ["result"] = new JsonObject
        {
            ["typeName"] = "Source",
            ["memoryReference"] = new JsonObject { ["id"] = "result" }
        }
    };

    public OutputsTabConverterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IVariableTypeService>(new VariableTypeServiceStub());
        Services.AddSingleton<IOutputConverterService>(_converterService);
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void LoadsCompatibleConvertersUsingTheDeclaredBindingTypes()
    {
        var cut = RenderOutputsTab();

        Assert.Equal(("Source", "String"), _converterService.Requests.Single());
        cut.WaitForAssertion(() => Assert.Contains(
            cut.FindComponents<MudSelectItem<string>>(),
            item => item.Instance.Value == "sample.to-text"));
    }

    [Fact]
    public async Task SelectingAndClearingAConverterRoundTripsOnlyConverterJson()
    {
        var cut = RenderOutputsTab();
        cut.WaitForState(() => cut.FindComponents<MudSelect<string>>().Count == 1);
        var converter = cut.FindComponents<MudSelect<string>>().Single();

        await cut.InvokeAsync(() => converter.Instance.ValueChanged.InvokeAsync("sample.to-text"));

        var binding = _activity["result"]!.AsObject();
        Assert.Equal("Source", binding["typeName"]!.GetValue<string>());
        Assert.Equal("result", binding["memoryReference"]!["id"]!.GetValue<string>());
        Assert.Equal("sample.to-text", binding["converter"]!["id"]!.GetValue<string>());
        Assert.Empty(binding["converter"]!["settings"]!.AsObject());

        await cut.InvokeAsync(() => converter.Instance.ValueChanged.InvokeAsync(string.Empty));

        Assert.Null(_activity["result"]!["converter"]);
    }

    [Fact]
    public void UnavailableDiscoveryDoesNotDiscardPersistedConverterConfiguration()
    {
        _activity["result"]!.AsObject()["converter"] = new JsonObject
        {
            ["id"] = "unavailable.converter",
            ["settings"] = new JsonObject { ["format"] = "compact" }
        };
        _converterService.Exception = new InvalidOperationException();

        var cut = RenderOutputsTab();

        Assert.DoesNotContain("Converter", cut.Markup);
        Assert.Equal("unavailable.converter", _activity["result"]!["converter"]!["id"]!.GetValue<string>());
        Assert.Equal("compact", _activity["result"]!["converter"]!["settings"]!["format"]!.GetValue<string>());
    }

    [Fact]
    public async Task ChangingDestinationClearsAnIncompatibleConverter()
    {
        _activity["result"]!.AsObject()["converter"] = new JsonObject { ["id"] = "sample.to-text" };
        var cut = RenderOutputsTab(workflowDefinition: new WorkflowDefinition
        {
            Variables =
            [
                new Variable { Id = "result", Name = "Result", TypeName = "String" },
                new Variable { Id = "count", Name = "Count", TypeName = "Integer" }
            ]
        });
        var bindingTarget = cut.FindComponent<MudSelect<BindingTargetOption>>();

        await cut.InvokeAsync(() => bindingTarget.Instance.ValueChanged.InvokeAsync(new BindingTargetOption("Count", "count", "Integer", false)));

        Assert.Null(_activity["result"]!["converter"]);
    }

    [Fact]
    public void ReadOnlyWorkspaceDisablesConverterSelection()
    {
        var cut = RenderOutputsTab(readOnly: true);

        Assert.All(cut.FindComponents<MudSelect<string>>(), select => Assert.True(select.Instance.Disabled));
    }

    [Fact]
    public void OutputsWithTheSameTypePairShareOneRequest()
    {
        _activity["summary"] = new JsonObject
        {
            ["typeName"] = "Source",
            ["memoryReference"] = new JsonObject { ["id"] = "result" }
        };

        RenderOutputsTab(outputs:
        [
            new OutputDescriptor { Name = "Result", TypeName = "Source", DisplayName = "Result" },
            new OutputDescriptor { Name = "Summary", TypeName = "Source", DisplayName = "Summary" }
        ]);

        Assert.Equal([("Source", "String")], _converterService.Requests);
    }

    [Fact]
    public async Task DistinctTypePairsStartConcurrently()
    {
        var allRequestsStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRequests = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _converterService.Handler = async (_, _, _) =>
        {
            if (_converterService.Requests.Count == 2)
                allRequestsStarted.TrySetResult();

            await releaseRequests.Task;
            return [];
        };
        _activity["summary"] = new JsonObject
        {
            ["typeName"] = "OtherSource",
            ["memoryReference"] = new JsonObject { ["id"] = "result" }
        };

        var cut = RenderOutputsTab(outputs:
        [
            new OutputDescriptor { Name = "Result", TypeName = "Source", DisplayName = "Result" },
            new OutputDescriptor { Name = "Summary", TypeName = "OtherSource", DisplayName = "Summary" }
        ]);

        await allRequestsStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, _converterService.Requests.Count);
        releaseRequests.SetResult();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindComponents<MudSelect<string>>().Count));
    }

    [Fact]
    public void UnchangedParametersReuseTheSuccessfulRequest()
    {
        var cut = RenderOutputsTab();
        cut.WaitForAssertion(() => Assert.Single(_converterService.Requests));

        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, cut.Instance.WorkflowDefinition)
            .Add(x => x.Activity, cut.Instance.Activity)
            .Add(x => x.ActivityDescriptor, cut.Instance.ActivityDescriptor)
            .Add(x => x.OnActivityUpdated, cut.Instance.OnActivityUpdated));

        Assert.Single(_converterService.Requests);
    }

    [Fact]
    public async Task SwitchingTargetsWithTheSameDeclaredTypeReusesTheRequest()
    {
        var cut = RenderOutputsTab(workflowDefinition: new WorkflowDefinition
        {
            Variables =
            [
                new Variable { Id = "result", Name = "Result", TypeName = "String" },
                new Variable { Id = "summary", Name = "Summary", TypeName = "String" }
            ]
        });
        var bindingTarget = cut.FindComponent<MudSelect<BindingTargetOption>>();

        await cut.InvokeAsync(() => bindingTarget.Instance.ValueChanged.InvokeAsync(new BindingTargetOption("Summary", "summary", "String", false)));

        Assert.Single(_converterService.Requests);
    }

    [Fact]
    public void FailedRequestsAreRetried()
    {
        _converterService.Exception = new InvalidOperationException();
        var cut = RenderOutputsTab();
        Assert.Single(_converterService.Requests);

        _converterService.Exception = null;
        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, cut.Instance.WorkflowDefinition)
            .Add(x => x.Activity, cut.Instance.Activity)
            .Add(x => x.ActivityDescriptor, cut.Instance.ActivityDescriptor)
            .Add(x => x.OnActivityUpdated, cut.Instance.OnActivityUpdated));

        cut.WaitForAssertion(() => Assert.Equal(2, _converterService.Requests.Count));
    }

    [Fact]
    public void EmptyResultsAreCached()
    {
        var cut = RenderOutputsTab(workflowDefinition: new WorkflowDefinition
        {
            Variables = [new Variable { Id = "result", Name = "Result", TypeName = "Integer" }]
        });
        cut.WaitForAssertion(() => Assert.Single(_converterService.Requests));

        cut.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, cut.Instance.WorkflowDefinition)
            .Add(x => x.Activity, cut.Instance.Activity)
            .Add(x => x.ActivityDescriptor, cut.Instance.ActivityDescriptor)
            .Add(x => x.OnActivityUpdated, cut.Instance.OnActivityUpdated));

        Assert.Single(_converterService.Requests);
    }

    [Fact]
    public async Task SelectingAConverterPersistsTopLevelSchemaDefaults()
    {
        using var schema = JsonDocument.Parse("""
            {"type":"object","properties":{"format":{"type":"string","default":"compact"},"precision":{"type":"integer","default":2},"enabled":{"type":"boolean","default":true}}}
            """);
        _converterService.Descriptors =
        [
            new OutputConverterDescriptor
            {
                Id = "sample.to-text",
                SourceTypeName = "Source",
                ResultTypeName = "String",
                SettingsSchema = schema.RootElement.Clone()
            }
        ];
        var updates = 0;
        var cut = RenderOutputsTab(onActivityUpdated: _ =>
        {
            updates++;
            return Task.CompletedTask;
        });
        cut.WaitForState(() => cut.FindComponents<MudSelect<string>>().Count == 1);

        await cut.InvokeAsync(() => cut.FindComponent<MudSelect<string>>().Instance.ValueChanged.InvokeAsync("sample.to-text"));

        var settings = _activity["result"]!["converter"]!["settings"]!.AsObject();
        Assert.Equal("compact", settings["format"]!.GetValue<string>());
        Assert.Equal(2, settings["precision"]!.GetValue<int>());
        Assert.True(settings["enabled"]!.GetValue<bool>());
        Assert.Equal(1, updates);
    }

    [Fact]
    public async Task ReselectingAConverterPreservesItsSettings()
    {
        _activity["result"]!.AsObject()["converter"] = new JsonObject
        {
            ["id"] = "sample.to-text",
            ["settings"] = new JsonObject { ["format"] = "custom" }
        };
        var cut = RenderOutputsTab();
        cut.WaitForState(() => cut.FindComponents<MudSelect<string>>().Count == 1);

        await cut.InvokeAsync(() => cut.FindComponent<MudSelect<string>>().Instance.ValueChanged.InvokeAsync("sample.to-text"));

        Assert.Equal("custom", _activity["result"]!["converter"]!["settings"]!["format"]!.GetValue<string>());
    }

    [Fact]
    public async Task CompletionAfterDisposalDoesNotMutateTheActivity()
    {
        _activity["result"]!.AsObject()["converter"] = new JsonObject
        {
            ["id"] = "sample.to-text",
            ["settings"] = new JsonObject { ["format"] = "custom" }
        };
        var releaseRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken requestCancellationToken = default;
        _converterService.Handler = async (sourceType, destinationType, cancellationToken) =>
        {
            if (destinationType == "String")
                return [CreateDescriptor("sample.to-text", sourceType, destinationType)];

            requestCancellationToken = cancellationToken;
            await releaseRequest.Task;
            return [];
        };
        var updates = 0;
        var cut = RenderOutputsTab(
            workflowDefinition: new WorkflowDefinition
            {
                Variables =
                [
                    new Variable { Id = "result", Name = "Result", TypeName = "String" },
                    new Variable { Id = "count", Name = "Count", TypeName = "Integer" }
                ]
            },
            onActivityUpdated: _ =>
            {
                updates++;
                return Task.CompletedTask;
            });
        var bindingTarget = cut.FindComponent<MudSelect<BindingTargetOption>>();

        var destinationChange = cut.InvokeAsync(() => bindingTarget.Instance.ValueChanged.InvokeAsync(
            new BindingTargetOption("Count", "count", "Integer", false)));
        cut.WaitForAssertion(() => Assert.Equal(2, _converterService.Requests.Count));
        cut.Instance.Dispose();

        Assert.True(requestCancellationToken.IsCancellationRequested);
        releaseRequest.SetResult();
        await destinationChange;
        Assert.Equal("sample.to-text", _activity["result"]!["converter"]!["id"]!.GetValue<string>());
        Assert.Equal(1, updates);
    }

    [Fact]
    public async Task ChangingTypePairsHidesStaleDescriptorsWhileLoading()
    {
        var newPairStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseNewPair = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _converterService.Handler = async (sourceType, destinationType, _) =>
        {
            if (destinationType == "String")
                return [CreateDescriptor("sample.to-text", sourceType, destinationType)];

            newPairStarted.TrySetResult();
            await releaseNewPair.Task;
            return [CreateDescriptor("sample.to-number", sourceType, destinationType)];
        };
        var cut = RenderOutputsTab(workflowDefinition: new WorkflowDefinition
        {
            Variables =
            [
                new Variable { Id = "result", Name = "Result", TypeName = "String" },
                new Variable { Id = "count", Name = "Count", TypeName = "Integer" }
            ]
        });
        var bindingTarget = cut.FindComponent<MudSelect<BindingTargetOption>>();

        var destinationChange = cut.InvokeAsync(() => bindingTarget.Instance.ValueChanged.InvokeAsync(
            new BindingTargetOption("Count", "count", "Integer", false)));
        await newPairStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Empty(cut.FindComponents<MudSelect<string>>());
        releaseNewPair.SetResult();
        await destinationChange;
        cut.WaitForAssertion(() =>
        {
            var option = Assert.Single(cut.FindComponents<MudSelectItem<string>>(), item => item.Instance.Value == "sample.to-number");
            Assert.NotNull(option);
            Assert.DoesNotContain(cut.FindComponents<MudSelectItem<string>>(), item => item.Instance.Value == "sample.to-text");
        });
    }

    [Fact]
    public async Task FailedTypePairRequestKeepsPersistedConverterButHidesStaleDescriptors()
    {
        _activity["result"]!.AsObject()["converter"] = new JsonObject { ["id"] = "sample.to-text" };
        _converterService.Handler = (sourceType, destinationType, _) => destinationType == "String"
            ? Task.FromResult<ICollection<OutputConverterDescriptor>>([CreateDescriptor("sample.to-text", sourceType, destinationType)])
            : Task.FromException<ICollection<OutputConverterDescriptor>>(new InvalidOperationException());
        var cut = RenderOutputsTab(workflowDefinition: new WorkflowDefinition
        {
            Variables =
            [
                new Variable { Id = "result", Name = "Result", TypeName = "String" },
                new Variable { Id = "count", Name = "Count", TypeName = "Integer" }
            ]
        });
        var bindingTarget = cut.FindComponent<MudSelect<BindingTargetOption>>();

        await cut.InvokeAsync(() => bindingTarget.Instance.ValueChanged.InvokeAsync(
            new BindingTargetOption("Count", "count", "Integer", false)));

        Assert.Empty(cut.FindComponents<MudSelect<string>>());
        Assert.Equal("sample.to-text", _activity["result"]!["converter"]!["id"]!.GetValue<string>());
    }

    [Fact]
    public async Task SwitchingConvertersDiscardsOldSettingsAndUsesNewDefaults()
    {
        using var schema = JsonDocument.Parse("""
            {"type":"object","properties":{"format":{"type":"string","default":"new-default"}}}
            """);
        _activity["result"]!.AsObject()["converter"] = new JsonObject
        {
            ["id"] = "sample.first",
            ["settings"] = new JsonObject { ["obsolete"] = true }
        };
        _converterService.Descriptors =
        [
            CreateDescriptor("sample.first", "Source", "String"),
            CreateDescriptor("sample.second", "Source", "String", schema.RootElement.Clone())
        ];
        var cut = RenderOutputsTab();
        cut.WaitForState(() => cut.FindComponents<MudSelect<string>>().Count == 1);

        await cut.InvokeAsync(() => cut.FindComponent<MudSelect<string>>().Instance.ValueChanged.InvokeAsync("sample.second"));

        var settings = _activity["result"]!["converter"]!["settings"]!.AsObject();
        Assert.Equal("new-default", settings["format"]!.GetValue<string>());
        Assert.False(settings.ContainsKey("obsolete"));
    }

    private static OutputConverterDescriptor CreateDescriptor(
        string id,
        string sourceType,
        string destinationType,
        JsonElement? settingsSchema = null) => new()
    {
        Id = id,
        SourceTypeName = sourceType,
        ResultTypeName = destinationType,
        DisplayName = id,
        SettingsSchema = settingsSchema
    };

    private IRenderedComponent<OutputsTab> RenderOutputsTab(
        WorkflowDefinition? workflowDefinition = null,
        bool readOnly = false,
        IReadOnlyCollection<OutputDescriptor>? outputs = null,
        Func<JsonObject, Task>? onActivityUpdated = null) => Render<OutputsTab>(parameters => parameters
        .AddCascadingValue<IWorkspace>(new WorkspaceStub(readOnly))
        .Add(x => x.WorkflowDefinition, workflowDefinition ?? new WorkflowDefinition
        {
            Variables = [new Variable { Id = "result", Name = "Result", TypeName = "String" }]
        })
        .Add(x => x.Activity, _activity)
        .Add(x => x.ActivityDescriptor, new ActivityDescriptor
        {
            Outputs = outputs ?? [new OutputDescriptor { Name = "Result", TypeName = "Source", DisplayName = "Result" }]
        })
        .Add(x => x.OnActivityUpdated, onActivityUpdated ?? (_ => Task.CompletedTask)));

    private sealed class VariableTypeServiceStub : IVariableTypeService
    {
        public Task<IEnumerable<VariableTypeDescriptor>> GetVariableTypesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<VariableTypeDescriptor>>([new("Source", "Source", "Tests", null)]);
    }

    private sealed class OutputConverterServiceStub : IOutputConverterService
    {
        public ICollection<(string SourceType, string DestinationType)> Requests { get; } = [];
        public Exception? Exception { get; set; }
        public Func<string, string, CancellationToken, Task<ICollection<OutputConverterDescriptor>>>? Handler { get; set; }
        public ICollection<OutputConverterDescriptor>? Descriptors { get; set; }

        public Task<ICollection<OutputConverterDescriptor>> GetOutputConvertersAsync(string sourceType, string destinationType, CancellationToken cancellationToken = default)
        {
            Requests.Add((sourceType, destinationType));
            if (Handler != null)
                return Handler(sourceType, destinationType, cancellationToken);

            if (Exception != null)
                throw Exception;

            if (destinationType == "Integer")
                return Task.FromResult<ICollection<OutputConverterDescriptor>>([]);

            return Task.FromResult(Descriptors ??
                (ICollection<OutputConverterDescriptor>)
                [
                new OutputConverterDescriptor
                {
                    Id = "sample.to-text",
                    SourceTypeName = sourceType,
                    ResultTypeName = destinationType,
                    DisplayName = "Convert to text",
                    Description = "Formats the value as text."
                }
            ]);
        }
    }

    private sealed class WorkspaceStub(bool isReadOnly) : IWorkspace
    {
        public bool IsReadOnly { get; } = isReadOnly;
        public bool HasWorkflowEditPermission => !IsReadOnly;
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
