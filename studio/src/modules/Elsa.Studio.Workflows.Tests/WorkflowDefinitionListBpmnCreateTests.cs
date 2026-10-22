using System.Reflection;
using Bunit;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Bpmn.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionList;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using MudExtensions.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers creating a BPMN workflow from the workflow definition list: offered only when a BPMN designer is enabled, it
/// asks for a name, creates the workflow through <c>POST bpmn/definitions</c>, and opens the new workflow.
/// </summary>
public sealed class WorkflowDefinitionListBpmnCreateTests : BunitContext, IAsyncLifetime
{
    private const string GeneratedName = "Workflow 1";
    private const string ExistingName = "Order process";
    private readonly FakeBpmnXmlService _bpmnXml = new();
    private readonly List<string> _opened = [];
    private IRenderedComponent<MudPopoverProvider> _popovers = null!;
    private IRenderedComponent<MudDialogProvider> _dialogs = null!;

    public WorkflowDefinitionListBpmnCreateTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddMudExtensions();
        Services.AddLogging();
        Services.AddCoreInternal();
        Services.AddRemoteBackend();
        Services.AddWorkflowsModule();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<ITimeFormatter, TestTimeFormatter>();
        Services.AddSingleton<IBpmnXmlService>(_bpmnXml);
        Services.AddSingleton(DispatchProxy.Create<IWorkflowDefinitionService, WorkflowDefinitionServiceProxy>());
        Services.AddSingleton(DispatchProxy.Create<IRemoteFeatureProvider, EnabledRemoteFeatureProviderProxy>());
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact(DisplayName = "Without a BPMN designer, creating a BPMN workflow is not offered")]
    public void WithoutADesigner_IsNotOffered()
    {
        var items = OpenCreateMenu(RenderList());

        Assert.Contains(items, item => item.TextContent.Contains("Import BPMN", StringComparison.Ordinal));
        Assert.DoesNotContain(items, item => item.TextContent.Contains("Create BPMN workflow", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "With a BPMN designer, the named workflow is created through POST bpmn/definitions and opened")]
    public void WithADesigner_CreatesAndOpensTheWorkflow()
    {
        EnableDesigner();
        _bpmnXml.CreateResult = new(new BpmnXmlSaveResult(new() { DefinitionId = "new-process" }, "\"REVISION-1\"", []));

        CreateNamed("Order handling");

        _dialogs.WaitForAssertion(() => Assert.Equal(["Order handling"], _bpmnXml.Creates));
        Assert.Equal(["new-process"], _opened);
    }

    [Fact(DisplayName = "A refused create says why, and opens nothing")]
    public void ARefusedCreate_IsReported()
    {
        EnableDesigner();
        _bpmnXml.CreateResult = new(new BpmnDocumentFailure(BpmnDocumentFailureReason.Unknown, "the server is read-only"));

        CreateNamed("Order handling");

        _dialogs.WaitForAssertion(() => Assert.Contains(Services.GetRequiredService<ISnackbar>().ShownSnackbars, snackbar => snackbar.Message.Contains("the server is read-only", StringComparison.Ordinal)));
        Assert.Empty(_opened);
    }

    [Fact(DisplayName = "Cancelling the dialog creates nothing")]
    public void Cancelling_CreatesNothing()
    {
        EnableDesigner();
        OpenCreateMenu(RenderList()).Single(item => item.TextContent.Contains("Create BPMN workflow", StringComparison.Ordinal)).Click();

        _dialogs.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll("[data-testid='bpmn-create-submit']")));
        _dialogs.FindAll("button").Single(button => button.TextContent.Trim() == "Cancel").Click();

        _dialogs.WaitForAssertion(() => Assert.Empty(_dialogs.FindAll("[data-testid='bpmn-create-submit']")));
        Assert.Empty(_bpmnXml.Creates);
        Assert.Empty(_opened);
    }

    private void EnableDesigner() => Services.AddBpmnDesigner(new BpmnDesignerDescriptor("test-designer", "_content/Test/designer.js"));

    private void CreateNamed(string name)
    {
        OpenCreateMenu(RenderList()).Single(item => item.TextContent.Contains("Create BPMN workflow", StringComparison.Ordinal)).Click();

        _dialogs.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll("[data-testid='bpmn-create-submit']")));
        Assert.Contains(GeneratedName, _dialogs.Find("[data-testid='bpmn-create-name']").OuterHtml);
        _dialogs.Find("[data-testid='bpmn-create-name']").Change(name);
        _dialogs.Find("[data-testid='bpmn-create-submit']").Click();
    }

    // Renders the providers here, not in the constructor, so a test can still enable a designer before the first render.
    private IRenderedComponent<WorkflowDefinitionList> RenderList()
    {
        _popovers = Render<MudPopoverProvider>();
        _dialogs = Render<MudDialogProvider>();
        var cut = Render<WorkflowDefinitionList>(parameters => parameters
            .AddCascadingValue(StubPermissionService.Grants("workflows/*:*"))
            .Add(x => x.EditWorkflowDefinition, definitionId => _opened.Add(definitionId)));
        cut.WaitForAssertion(() => Assert.Contains(ExistingName, cut.Markup));
        return cut;
    }

    private IReadOnlyList<AngleSharp.Dom.IElement> OpenCreateMenu(IRenderedComponent<WorkflowDefinitionList> cut) =>
        _popovers.OpenMenu(cut.Find(".mud-button-group-root .mud-menu button"));

    private class WorkflowDefinitionServiceProxy : DispatchProxy
    {
        private static readonly Link[] PublishLinks = [new("/publish", "publish", "POST"), new("/bulk-publish", "bulk-publish", "POST")];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod!.Name switch
        {
            // The publish links are what make the list editable rather than read-only.
            nameof(IWorkflowDefinitionService.ListAsync) => Task.FromResult(new PagedListResponse<WorkflowDefinitionSummary>
            {
                Items = [new() { Id = "order-process:1", DefinitionId = "order-process", Name = ExistingName, Version = 1, IsLatest = true, Links = PublishLinks }],
                TotalCount = 1,
                Links = PublishLinks
            }),
            nameof(IWorkflowDefinitionService.GenerateUniqueNameAsync) => Task.FromResult(GeneratedName),
            nameof(IWorkflowDefinitionService.GetIsNameUniqueAsync) => Task.FromResult(true),
            _ => throw new InvalidOperationException($"Unexpected call to {targetMethod.DeclaringType!.Name}.{targetMethod.Name}.")
        };
    }

    private class EnabledRemoteFeatureProviderProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod!.Name == nameof(IRemoteFeatureProvider.IsEnabledAsync)
                ? Task.FromResult(true)
                : throw new InvalidOperationException($"Unexpected call to {targetMethod.DeclaringType!.Name}.{targetMethod.Name}.");
    }
}
