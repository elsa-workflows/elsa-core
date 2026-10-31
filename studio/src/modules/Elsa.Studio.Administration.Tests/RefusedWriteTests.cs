using Bunit;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Alterations.Catalog;
using Elsa.Studio.Alterations.Components;
using Elsa.Studio.Alterations.Models;
using Elsa.Studio.Alterations.Services;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Refit;
using Xunit;
using LabelsPage = Elsa.Studio.Labels.UI.Pages.Labels;

namespace Elsa.Studio.Administration.Tests;

/// <summary>
/// Actions the user could start but the backend then refuses: the failure reads as guidance, not as the API client's
/// raw "Response status code does not indicate success: 403 (Forbidden).". Reads succeed here, so the pages load.
/// </summary>
public sealed class RefusedWriteTests : BunitContext, IAsyncLifetime
{
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;

    public RefusedWriteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer>(new TestLocalizer());
        Services.AddSingleton<IBackendApiClientProvider>(new ForbiddingBackend(readsSucceed: true));
        Services.AddSingleton<IAlterationCatalog, AlterationCatalog>();
        Services.AddScoped<IAlterationStagingService, AlterationStagingService>();
        Services.AddSingleton<IWorkflowInstanceService>(new NoVariablesInstanceService());
        Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    private ISnackbar Snackbar => Services.GetRequiredService<ISnackbar>();

    [Fact]
    public void LabelsPage_ShowsThePermissionGuidance_WhenTheBackendRefusesToCreateTheLabel()
    {
        var cut = Render<LabelsPage>(parameters => parameters.AddCascadingValue(StubPermissionService.Grants("labels:view", "labels:create")));
        cut.WaitForAssertion(() => Assert.Contains("No Labels found", cut.Markup));

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Create Label").Click();
        ClickWhenEnabled("Ok");

        cut.WaitForAssertion(() => Assert.Equal(AuthorizationFailureExtensions.ForbiddenMessage, Assert.Single(Snackbar.ShownSnackbars).Message));
    }

    [Fact]
    public async Task AlterationDryRun_ShowsThePermissionGuidance_WhenTheBackendRefusesIt()
    {
        var staging = Services.GetRequiredService<IAlterationStagingService>();
        var cut = Render<AlterationDesignerHost>(parameters => parameters.Add(x => x.WorkflowInstance, new WorkflowInstance { Id = "instance-1" }));
        await StageCancelAsync(staging);

        cut.WaitForElement(".elsa-alt-side-tabs button:nth-of-type(3)").Click();
        cut.WaitForElement(".elsa-alt-plan-footer button:not([disabled])").Click();

        cut.WaitForAssertion(() => Assert.Equal($"Dry-run failed: {AuthorizationFailureExtensions.ForbiddenMessage}", Assert.Single(Snackbar.ShownSnackbars).Message));
    }

    [Fact]
    public async Task AlterationSubmit_ShowsThePermissionGuidance_InsteadOfTheRawStatusAndBody()
    {
        var staging = Services.GetRequiredService<IAlterationStagingService>();
        await StageCancelAsync(staging);

        await Services.GetRequiredService<IDialogService>().ShowAsync<AlterationSubmitDialog>(null, new DialogParameters<AlterationSubmitDialog>
        {
            { x => x.WorkflowInstanceId, "instance-1" },
            { x => x.StagedItems, staging.Items }
        });
        ClickWhenEnabled("Next");
        ClickWhenEnabled("Submit");

        _dialogProvider.WaitForAssertion(() =>
        {
            Assert.Contains(AuthorizationFailureExtensions.ForbiddenMessage, _dialogProvider.Find(".elsa-alt-submit-dialog pre").TextContent);
            Assert.DoesNotContain("HTTP 403", _dialogProvider.Markup);
        });
    }

    private async Task StageCancelAsync(IAlterationStagingService staging)
    {
        var cancel = await Services.GetRequiredService<IAlterationCatalog>().FindAsync("Cancel");
        staging.Add(new StagedAlteration { Descriptor = cancel! });
    }

    private void ClickWhenEnabled(string label) => _dialogProvider.WaitForAssertion(() =>
    {
        var button = _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == label);
        Assert.False(button.HasAttribute("disabled"));
        button.Click();
    });

    private sealed class NoVariablesInstanceService : IWorkflowInstanceService
    {
        public Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(string instanceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<ResolvedVariable>>([]);

        public Task<PagedListResponse<WorkflowInstanceSummary>> ListAsync(ListWorkflowInstancesRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string instanceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task BulkDeleteAsync(IEnumerable<string> instanceIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CancelAsync(string instanceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task BulkCancelAsync(BulkCancelWorkflowInstancesRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkflowInstance?> GetAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PagedListResponse<WorkflowExecutionLogRecord>> GetJournalAsync(string instanceId, JournalFilter? filter = default, int? skip = default, int? take = default, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> ExportAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> BulkExportAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> BulkImportAsync(IEnumerable<StreamPart> streamParts, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
