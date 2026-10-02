using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionList;

/// Displays a list of workflow definitions.
public partial class WorkflowDefinitionList
{
    private MudTable<WorkflowDefinitionRow> _table = null!;
    private HashSet<WorkflowDefinitionRow> _selectedRows = new();
    private long _totalCount;
    private const int truncationLength = 40;

    /// An event that is invoked when a workflow definition is edited.
    [Parameter] public EventCallback<string> EditWorkflowDefinition { get; set; }

    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private IUserMessageService UserMessageService { get; set; } = null!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;
    [Inject] private IWorkflowDefinitionEditorService WorkflowDefinitionEditorService { get; set; } = null!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = null!;
    [Inject] private IWorkflowDefinitionImporter WorkflowDefinitionImporter { get; set; } = null!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = null!;
    [Inject] private IMediator Mediator { get; set; } = null!;
    [Inject] private ICreateWorkflowDialogComponentProvider CreateWorkflowDialogComponentProvider { get; set; } = null!;
    [Inject] private IWorkflowCloningDialogService WorkflowCloningService { get; set; } = null!;
    [Inject] private IWorkflowExportDialogService WorkflowExportDialogService { get; set; } = null!;
    [Inject] private IBpmnImportUiService BpmnImportUiService { get; set; } = null!;

    private string SearchTerm { get; set; } = string.Empty;
    private bool IsReadOnlyMode { get; set; }
    private string ReadonlyWorkflowsExcluded => Localizer["The read-only workflows will not be affected."];
    private IDictionary<string, object?> WorkflowDefinitionListBulkActionAttributes => new Dictionary<string, object?>
    {
        ["DefinitionIds"] = _selectedRows.Select(x => x.DefinitionId).ToList(),
        ["Disabled"] = IsReadOnlyMode || !(_selectedRows?.Any() ?? false)
    };

    private IDictionary<string, object?> GetWorkflowDefinitionListRowActionAttributes(WorkflowDefinitionRow row) => new Dictionary<string, object?>
    {
        ["DefinitionIds"] = new[] { row.DefinitionId },
        ["Disabled"] = IsReadOnlyMode || row.IsReadOnlyMode
    };

    private async Task<TableData<WorkflowDefinitionRow>> ServerReload(TableState state, CancellationToken cancellationToken)
    {
        var request = new ListWorkflowDefinitionsRequest
        {
            IsSystem = false,
            Page = state.Page,
            PageSize = state.PageSize,
            SearchTerm = SearchTerm,
            OrderBy = GetOrderBy(state.SortLabel),
            OrderDirection = state.SortDirection == SortDirection.Descending
                ? OrderDirection.Descending
                : OrderDirection.Ascending
        };

        var latestWorkflowDefinitionsResponse = await WorkflowDefinitionService.ListAsync(request, VersionOptions.Latest, cancellationToken);
        if (latestWorkflowDefinitionsResponse is null)
            return new TableData<WorkflowDefinitionRow> { TotalItems = 0, Items = [] };

        var latestWorkflowDefinitions = latestWorkflowDefinitionsResponse.Items;
        IsReadOnlyMode = (latestWorkflowDefinitionsResponse?.Links?.Count(l => l.Rel == "bulk-publish") ?? 0) == 0;
        var unpublishedWorkflowDefinitionIds = latestWorkflowDefinitions.Where(x => !x.IsPublished).Select(x => x.DefinitionId).ToList();

        var publishedWorkflowDefinitions = await WorkflowDefinitionService.ListAsync(new ListWorkflowDefinitionsRequest
        {
            DefinitionIds = unpublishedWorkflowDefinitionIds,
        }, VersionOptions.Published);

        _totalCount = latestWorkflowDefinitionsResponse!.TotalCount;

        var workflowDefinitionRows = latestWorkflowDefinitions
            .Select(definition =>
            {
                var latestVersionNumber = definition.Version;
                var isPublished = definition.IsPublished;
                var publishedVersion = isPublished
                    ? definition
                    : publishedWorkflowDefinitions.Items.FirstOrDefault(x => x.DefinitionId == definition.DefinitionId);
                var publishedVersionNumber = publishedVersion?.Version;

                return new WorkflowDefinitionRow(
                    definition.Id,
                    definition.DefinitionId,
                    latestVersionNumber,
                    publishedVersionNumber,
                    definition.Name,
                    definition.Description,
                    definition.IsPublished,
                    (definition?.Links?.Count(l => l.Rel == "publish") ?? 0) == 0);
            })
            .ToList();

        // Update initialPage to preserve current page during re-renders
        InitialPage = state.Page;

        // Update URL to reflect current table state & filters
        TryUpdateUrlFromState(state);

        return new TableData<WorkflowDefinitionRow>
        {
            TotalItems = (int)_totalCount,
            Items = workflowDefinitionRows
        };
    }

    /// <inheritdoc/>
    protected override async Task ApplyQueryParameters(IDictionary<string, string> query)
    {
        // Paging
        if (query.TryGetValue("page", out var pageValue) && int.TryParse(pageValue, out var page)) InitialPage = page - 1;
        if (query.TryGetValue("pageSize", out var pageSizeValue) && int.TryParse(pageSizeValue, out var pageSize)) _table.SetRowsPerPage(pageSize);

        // Filters
        if (query.TryGetValue("search", out var searchValue)) SearchTerm = searchValue;
    }

    /// <inheritdoc/>
    protected override Dictionary<string, string?> BuildQueryFromState(TableState state)
    {
        var query = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["page"] = (state.Page + 1).ToString(),
            ["pageSize"] = state.PageSize.ToString()
        };

        if (!string.IsNullOrWhiteSpace(SearchTerm)) query["search"] = SearchTerm;

        return query;
    }

    private OrderByWorkflowDefinition? GetOrderBy(string? sortLabel)
    {
        return sortLabel switch
        {
            "Name" => OrderByWorkflowDefinition.Name,
            "Version" => OrderByWorkflowDefinition.Version,
            "Created" => OrderByWorkflowDefinition.Created,
            _ => null
        };
    }

    private async Task OnCreateWorkflowClicked()
    {
        var workflowName = await WorkflowDefinitionService.GenerateUniqueNameAsync();

        var parameters = new DialogParameters<CreateWorkflowDialog>
        {
            { x => x.WorkflowName, workflowName }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };
        
        var dialogComponentType = CreateWorkflowDialogComponentProvider.GetComponentType();
        var dialogInstance = await DialogService.ShowAsync(dialogComponentType, Localizer["New workflow"], parameters, options);
        var dialogResult = await dialogInstance.Result;

        if (dialogResult?.Canceled != false || dialogResult.Data is not Result<WorkflowDefinition, ValidationErrors> result)
            return;

        await result.OnSuccessAsync(definition => EditAsync(definition.DefinitionId));
        result.OnFailed(errors => UserMessageService.ShowSnackbarTextMessage(string.Join(Environment.NewLine, errors.Errors)));
    }

    private async Task OnDuplicateWorkflowClicked(WorkflowDefinitionRow workflowDefinitionRow)
    {
        var originalDefinition = await WorkflowDefinitionService.FindByDefinitionIdAsync(workflowDefinitionRow.DefinitionId, VersionOptions.Latest);
        if (originalDefinition == null)
        {
            UserMessageService.ShowSnackbarTextMessage(Localizer["Original workflow definition not found."], Severity.Error);
            return;
        }

        var result = await WorkflowCloningService.Duplicate(originalDefinition);
        if (result is null) return;
        if (result.IsSuccess) Reload();
    }

    private async Task EditAsync(string definitionId)
    {
        await EditWorkflowDefinition.InvokeAsync(definitionId);
    }

    private void Reload()
    {
        _table.ReloadServerData();
    }

    private async Task OnEditClicked(string definitionId)
    {
        await EditAsync(definitionId);
    }

    private async Task OnRowClick(TableRowClickEventArgs<WorkflowDefinitionRow> e)
    {
        if (e.Item is not null)
            await EditAsync(e.Item.DefinitionId);
    }

    private async Task OnRunWorkflowClicked(WorkflowDefinitionRow workflowDefinitionRow)
    {
        var request = new ExecuteWorkflowDefinitionRequest
        {
            VersionOptions = VersionOptions.Latest
        };

        var definitionId = workflowDefinitionRow!.DefinitionId;
        var response = await WorkflowDefinitionService.ExecuteAsync(definitionId, request);

        if (response.CannotStart)
        {
            UserMessageService.ShowSnackbarTextMessage(Localizer["The workflow cannot be started"], Severity.Error);
            return;
        }

        UserMessageService.ShowSnackbarTextMessage(Localizer["Successfully started workflow"], Severity.Success);
    }

    private async Task OnDeleteClicked(WorkflowDefinitionRow workflowDefinitionRow)
    {
        var result = await DialogService.ShowMessageBoxAsync(Localizer["Delete workflow?"],
            Localizer["Are you sure you want to delete this workflow?"], yesText: Localizer["Delete"], cancelText: Localizer["Cancel"]);

        if (result != true)
            return;

        var definitionId = workflowDefinitionRow.DefinitionId;
        await WorkflowDefinitionService.DeleteAsync(definitionId);
        Reload();
    }

    private async Task OnCancelClicked(WorkflowDefinitionRow workflowDefinitionRow)
    {
        var result = await DialogService.ShowMessageBoxAsync(Localizer["Cancel running workflow instances?"],
            Localizer["Are you sure you want to cancel all running workflow instances of this workflow definition?"], yesText: Localizer["Yes"], cancelText: Localizer["No"]);

        if (result != true)
            return;

        var request = new BulkCancelWorkflowInstancesRequest
        {
            DefinitionVersionId = workflowDefinitionRow.Id
        };
        await WorkflowInstanceService.BulkCancelAsync(request);
        Reload();
    }

    private async Task OnExportClicked(WorkflowDefinitionRow workflowDefinitionRow)
    {
        await WorkflowExportDialogService.ExportAndDownloadAsync(include =>
            WorkflowDefinitionService.ExportDefinitionAsync(workflowDefinitionRow.DefinitionId, VersionOptions.Latest, include));
    }

    private async Task OnBulkDeleteClicked()
    {
        var result = await DialogService.ShowMessageBoxAsync(
    Localizer["Delete selected workflows?"],
    Localizer["Are you sure you want to delete the selected workflows?"] + (_selectedRows.Count(w => w.IsReadOnlyMode) > 0 ? ReadonlyWorkflowsExcluded : ""), yesText: Localizer["Delete"], cancelText: Localizer["Cancel"]);

        if (result != true)
            return;

        var workflowDefinitionIds = _selectedRows.Select(x => x.DefinitionId).ToList();
        await WorkflowDefinitionService.BulkDeleteAsync(workflowDefinitionIds);
        _selectedRows.Clear();
        Reload();
    }

    private async Task OnBulkPublishClicked()
    {
        var result = await DialogService.ShowMessageBoxAsync(Localizer["Publish selected workflows?"],
            Localizer["Are you sure you want to publish the selected workflows?"] + (_selectedRows.Count(w => w.IsReadOnlyMode) > 0 ? ReadonlyWorkflowsExcluded : ""), yesText: Localizer["Publish"], cancelText: Localizer["Cancel"]);

        if (result != true)
            return;

        var workflowDefinitionIds = _selectedRows.Select(x => x.DefinitionId).ToList();
        var response = await WorkflowDefinitionService.BulkPublishAsync(workflowDefinitionIds);

        if (response.Published.Count > 0)
        {
            var message = response.Published.Count == 1
                ? Localizer["One workflow is published"]
                : Localizer["{0} workflows are published", response.Published.Count];
            UserMessageService.ShowSnackbarTextMessage(message, Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        }

        if (response.AlreadyPublished.Count > 0)
        {
            var message = response.AlreadyPublished.Count == 1
                ? Localizer["One workflow is already published"]
                : Localizer["{0} workflows are already published", response.AlreadyPublished.Count];
            UserMessageService.ShowSnackbarTextMessage(message, Severity.Info, options => { options.SnackbarVariant = Variant.Filled; });
        }

        if (response.UpdatedConsumers.Count > 0)
        {
            var message = response.UpdatedConsumers.Count == 1
                ? Localizer["One workflow consuming a published workflow has been updated"]
                : Localizer["{0} workflows consuming published workflows have been updated", response.UpdatedConsumers.Count];
            UserMessageService.ShowSnackbarTextMessage(message, Severity.Info, options =>
            {
                options.SnackbarVariant = Variant.Filled;
            });
        }

        if (response.NotFound.Count > 0)
        {
            var message = response.NotFound.Count == 1
                ? Localizer["One workflow is not found"]
                : Localizer["{0} workflows are not found", response.NotFound.Count];
            UserMessageService.ShowSnackbarTextMessage(message, Severity.Warning, options => { options.SnackbarVariant = Variant.Filled; });
        }
        _selectedRows.Clear();
        Reload();
    }

    private async Task OnBulkRetractClicked()
    {
        var result = await DialogService.ShowMessageBoxAsync(Localizer["Unpublish selected workflows?"],
            Localizer["Are you sure you want to unpublish the selected workflows?"] + (_selectedRows.Count(w => w.IsReadOnlyMode) > 0 ? ReadonlyWorkflowsExcluded : ""), yesText: Localizer["Unpublish"], cancelText: Localizer["Cancel"]);

        if (result != true)
            return;

        var workflowDefinitionIds = _selectedRows.Select(x => x.DefinitionId).ToList();
        var response = await WorkflowDefinitionService.BulkRetractAsync(workflowDefinitionIds);

        if (response.Retracted.Count > 0)
        {
            var message = response.Retracted.Count == 1
                ? Localizer["One workflow is unpublished"]
                : Localizer["{0} workflows are unpublished", response.Retracted.Count];
            UserMessageService.ShowSnackbarTextMessage(message, Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        }

        if (response.AlreadyRetracted.Count > 0)
        {
            var message = response.AlreadyRetracted.Count == 1
                ? Localizer["One workflow is already unpublished"]
                : Localizer["{0} workflows are already unpublished", response.AlreadyRetracted.Count];
            UserMessageService.ShowSnackbarTextMessage(message, Severity.Info, options => { options.SnackbarVariant = Variant.Filled; });
        }

        if (response.NotFound.Count > 0)
        {
            var message = response.NotFound.Count == 1
                ? Localizer["One workflow is not found"]
                : Localizer["{0} workflows are not found", response.NotFound.Count];
            UserMessageService.ShowSnackbarTextMessage(message, Severity.Warning, options => { options.SnackbarVariant = Variant.Filled; });
        }
        _selectedRows.Clear();
        Reload();
    }

    private async Task OnBulkExportClicked()
    {
        var workflowVersionIds = _selectedRows.Select(x => x.Id).ToList();
        var exported = await WorkflowExportDialogService.ExportAndDownloadAsync(include =>
            WorkflowDefinitionService.BulkExportDefinitionsAsync(workflowVersionIds, include));

        if (!exported)
            return;

        _selectedRows.Clear();
        Reload();
    }

    private Task OnImportClicked()
    {
        return DomAccessor.ClickElementAsync("#workflow-file-upload-button-wrapper input[type=file]");
    }

    private Task OnImportBpmnClicked()
    {
        return DomAccessor.ClickElementAsync("#workflow-bpmn-file-upload-button-wrapper input[type=file]");
    }

    private async Task OnBpmnFilesSelected(IReadOnlyList<IBrowserFile> files)
    {
        if (files.Count == 0)
            return;

        var result = await BpmnImportUiService.ImportFileAsync(files[0], definitionId: null);

        // A null result means the user cancelled the findings dialog before anything was imported; there is
        // nothing to report or reload.
        if (result == null)
            return;

        if (result.IsSuccess && result.WorkflowDefinition is { } workflowDefinition)
        {
            UserMessageService.ShowSnackbarTextMessage(Localizer["Workflow imported successfully."], Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
            await EditAsync(workflowDefinition.DefinitionId);
        }
        else if (result.Failure is { } failure && failure.FailureType != WorkflowImportFailureType.CapabilityRefusal)
        {
            // A capability refusal was already reported in its own dialog by BpmnImportUiService; a snackbar here
            // would just repeat it.
            UserMessageService.ShowSnackbarTextMessage(Localizer["Failed to import BPMN document. Reason: {0}", failure.ErrorMessage], Severity.Error, options =>
            {
                options.SnackbarVariant = Variant.Filled;
                options.VisibleStateDuration = 10000;
            });
        }

        Reload();
    }

    private async Task OnFilesSelected(IReadOnlyList<IBrowserFile> files)
    {
        // A .bpmn file dropped on the generic (JSON/ZIP) picker cannot be parsed as either, so it is routed through
        // the same interactive BPMN import flow the dedicated "Import BPMN" button uses instead of silently
        // producing zero results.
        var bpmnBatch = await BpmnImportUiService.ImportBpmnFilesAsync(files, definitionId: null);

        var results = new List<WorkflowImportResult>(bpmnBatch.Results);

        if (bpmnBatch.OtherFiles.Count > 0)
            results.AddRange(await WorkflowDefinitionImporter.ImportFilesAsync(bpmnBatch.OtherFiles));

        var reportableResults = BpmnImportBatch.ReportableResults(results);

        if (BpmnImportBatch.AllReported(results))
        {
            // Every result was a capability refusal, each already explained in its own dialog; nothing left to summarize.
            Reload();
            return;
        }

        var successfulResultCount = reportableResults.Count(x => x.IsSuccess);
        var failedResultCount = reportableResults.Count(x => !x.IsSuccess);
        var successfulWorkflowsTerm = successfulResultCount == 1 ? "workflow" : "workflows";
        var failedWorkflowsTerm = failedResultCount == 1 ? Localizer["workflow"] : Localizer["workflows"];
        var reasons = string.Join(", ", reportableResults.Where(x => x.Failure != null).Select(x => x.Failure!.ErrorMessage));
        var message = reportableResults.Count == 0 ? Localizer["No workflows found to import."] :
            successfulResultCount > 0 && failedResultCount == 0 ? Localizer["{0} {1} imported successfully.", successfulResultCount, successfulWorkflowsTerm] :
            successfulResultCount == 0 && failedResultCount > 0 ? Localizer["Failed to import {0} {1}. Reason: {2}", failedResultCount, failedWorkflowsTerm, reasons] : Localizer["{0} {1} imported successfully.", successfulResultCount, successfulWorkflowsTerm] + " " + Localizer["Failed to import {0} {1}. Reasons: {2}", failedResultCount, failedWorkflowsTerm, reasons];
        var severity = reportableResults.Count == 0 ? Severity.Info : successfulResultCount > 0 && failedResultCount > 0 ? Severity.Warning : failedResultCount == 0 ? Severity.Success : Severity.Error;
        UserMessageService.ShowSnackbarTextMessage(message, severity, options =>
        {
            options.SnackbarVariant = Variant.Filled;
            options.CloseAfterNavigation = failedResultCount > 0;
            options.VisibleStateDuration = failedResultCount > 0 ? 10000 : 5000;
        });
        Reload();
    }

    private void OnSearchTermChanged(string text)
    {
        if (SearchTerm != text)
        {
            SearchTerm = text;
            Reload();
        }
    }

    private async Task OnPublishClicked(string definitionId)
    {
        var response = await WorkflowDefinitionService.PublishAsync(definitionId);
        if (response.AlreadyPublished)
        {
            UserMessageService.ShowSnackbarTextMessage(Localizer["Workflow was already published"], Severity.Info, options => { options.SnackbarVariant = Variant.Filled; });
        }
        else
        {
            UserMessageService.ShowSnackbarTextMessage(Localizer["Workflow published"], Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        }

        if (response.ConsumingWorkflowCount > 0)
        {
            var message = response.ConsumingWorkflowCount == 1
                ? Localizer["One workflow consuming a published workflow has been updated"]
                : Localizer["{0} workflows consuming published workflows have been updated", response.ConsumingWorkflowCount];
            UserMessageService.ShowSnackbarTextMessage(message, Severity.Info, options =>
            {
                options.SnackbarVariant = Variant.Filled;
            });
        }

        Reload();
    }

    private async Task OnRetractClicked(string definitionId)
    {
        await WorkflowDefinitionService.RetractAsync(definitionId);
        UserMessageService.ShowSnackbarTextMessage(Localizer["Workflow retracted"], Severity.Success, options => { options.SnackbarVariant = Variant.Filled; });
        Reload();
    }

    private async Task OnViewDescriptionClicked(string value)
    {
        DialogParameters<MarkdownEditor> param = new()
        {
            { x => x.Label, "Description" },
            { x => x.Value, value },
            { x => x.IsReadOnly, true }
        };
        DialogOptions options = new()
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            FullWidth = true,
            FullScreen = false,
            MaxWidth = MaxWidth.Large
        };
        await DialogService.ShowAsync<MarkdownEditor>("Description", param, options);
    }


    private record WorkflowDefinitionRow(
        string Id,
        string DefinitionId,
        int LatestVersion,
        int? PublishedVersion,
        string? Name,
        string? Description,
        bool IsPublished,
        bool IsReadOnlyMode);
}
