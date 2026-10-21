using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class InputsTabReadOnlyWorkspaceTests : BunitContext
{
    private readonly MutableWorkspaceStub _workspace = new() { IsReadOnly = true };
    private readonly WorkflowDefinition _workflowDefinition = new()
    {
        Id = "workflow-1",
        DefinitionId = "workflow-1",
        Root = new JsonObject()
    };
    private readonly JsonObject _activity = new()
    {
        ["id"] = "activity-1",
        ["value"] = "original"
    };
    private readonly ActivityDescriptor _activityDescriptor = new()
    {
        TypeName = "Tests.InputActivity",
        Inputs =
        [
            new InputDescriptor
            {
                Name = "Value",
                TypeName = "System.String",
                IsBrowsable = true,
                UIHint = "test-input"
            }
        ]
    };

    public InputsTabReadOnlyWorkspaceTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<IUIHintService>(new TestUIHintService());
        Services.AddSingleton<IBackendApiClientProvider>(new UnusedBackendApiClientProvider());
    }

    [Fact]
    public async Task WorkspaceReadOnlyChangesRebuildCachedEditorsWithoutLosingInputValue()
    {
        var cut = RenderInputsTab();

        cut.WaitForAssertion(() => AssertEditorIsReadOnly(cut, expected: true, expectedValue: "original"));

        _workspace.IsReadOnly = false;
        SetInputsTabParameters(cut);
        cut.WaitForAssertion(() => AssertEditorIsReadOnly(cut, expected: false, expectedValue: "original"));

        await cut.Find("[data-testid=input-editor]").ChangeAsync("edited while writable");
        Assert.Equal("edited while writable", _activity["value"]!.GetValue<string>());

        _workspace.IsReadOnly = true;
        SetInputsTabParameters(cut);
        cut.WaitForAssertion(() => AssertEditorIsReadOnly(cut, expected: true, expectedValue: "edited while writable"));
    }

    private IRenderedComponent<InputsTab> RenderInputsTab() => Render<InputsTab>(CreateParameters());

    private void SetInputsTabParameters(IRenderedComponent<InputsTab> cut) => cut.SetParametersAndRender(CreateParameters());

    private Action<ComponentParameterCollectionBuilder<InputsTab>> CreateParameters() => parameters => parameters
        .AddCascadingValue<IWorkspace>(_workspace)
        .Add(x => x.WorkflowDefinition, _workflowDefinition)
        .Add(x => x.Activity, _activity)
        .Add(x => x.ActivityDescriptor, _activityDescriptor)
        .Add(x => x.OnActivityUpdated, _ => Task.CompletedTask);

    private static void AssertEditorIsReadOnly(IRenderedComponent<InputsTab> cut, bool expected, string expectedValue)
    {
        var editor = cut.Find("[data-testid=input-editor]");
        Assert.Equal(expected, editor.HasAttribute("disabled"));
        Assert.Equal(expectedValue, editor.GetAttribute("value"));
    }

    private sealed class MutableWorkspaceStub : IWorkspace
    {
        public bool IsReadOnly { get; set; }
        public bool HasWorkflowEditPermission => !IsReadOnly;
    }

    private sealed class TestUIHintService : IUIHintService
    {
        private readonly IUIHintHandler _handler = new TestUIHintHandler();

        public IUIHintHandler GetHandler(string uiHint) => _handler;
    }

    private sealed class TestUIHintHandler : IUIHintHandler
    {
        public bool GetSupportsUIHint(string uiHint) => uiHint == "test-input";

        public string UISyntax => string.Empty;

        public RenderFragment DisplayInputEditor(DisplayInputEditorContext context) => builder =>
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "data-testid", "input-editor");
            builder.AddAttribute(2, "value", context.Value?.ToString());
            if (context.IsReadOnly)
                builder.AddAttribute(3, "disabled", true);
            builder.AddAttribute(4, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, args => context.OnValueChanged(args.Value)));
            builder.CloseElement();
        };
    }

    private sealed class UnusedBackendApiClientProvider : IBackendApiClientProvider
    {
        public Uri Url => new("http://localhost");

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class =>
            throw new NotSupportedException("This test does not refresh input descriptors.");
    }
}
