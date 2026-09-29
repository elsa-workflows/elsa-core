using Elsa.Studio.Contracts;
using Elsa.Studio.Labels.Client;
using Elsa.Studio.Labels.Models;
using Elsa.Studio.Labels.UI.Components;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;

using LabelModel = Elsa.Studio.Labels.Models.Label;

namespace Elsa.Studio.Labels.UI.Pages;

/// <summary>
/// Represents the Labels page, which provides functionality for managing labels.
/// </summary>
[UsedImplicitly]
public partial class Labels
{
    private MudTable<LabelModel> _table = null!;
    private HashSet<LabelModel> _selectedRows = new();

    /// <summary>
    /// Gets or sets the dialog service for displaying dialogs.
    /// </summary>
    [Inject] private IDialogService DialogService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the snackbar service for displaying notifications.
    /// </summary>
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    /// <summary>
    /// Gets or sets the navigation manager for handling navigation.
    /// </summary>
    [Inject] NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// Gets or sets the API client provider for accessing backend APIs.
    /// </summary>
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = default!;

    /// <summary>
    /// Retrieves the labels API client.
    /// </summary>
    /// <returns>An instance of <see cref="ILabelsApi"/>.</returns>
    private async Task<ILabelsApi> GetApiAsync()
    {
        return await ApiClientProvider.GetApiAsync<ILabelsApi>();
    }

    /// <summary>
    /// Reloads the table data from the server.
    /// </summary>
    /// <param name="state">The table state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The table data.</returns>
    private async Task<TableData<LabelModel>> ServerReload(TableState state, CancellationToken cancellationToken)
    {
        var apiClient = await GetApiAsync();
        var labels = await apiClient.ListAsync(cancellationToken);

        return new TableData<LabelModel>
        {
            TotalItems = (int)labels.Count,
            Items = labels.Items
        };
    }

    /// <summary>
    /// Handles the creation of a new label.
    /// </summary>
    private async Task OnCreateClicked()
    {
        var apiClient = await GetApiAsync();
        var parameters = new DialogParameters<CreateLabelDialog>
        {
            { x => x.LabelName, "LabelName" }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };

        var dialogInstance = await DialogService.ShowAsync<CreateLabelDialog>("New Label", parameters, options);
        var dialogResult = await dialogInstance.Result;
        if (dialogResult is { Canceled: false, Data: LabelInputModel inputModel })
        {
            try
            {
                var model = await apiClient.CreateAsync(inputModel);
                await EditAsync(model.Id);
                Reload();
            }
            catch (Exception e)
            {
                Snackbar.Add(e.Message, Severity.Error);
            }
        }
    }

    /// <summary>
    /// Navigates to the edit page for the specified label.
    /// </summary>
    /// <param name="id">The ID of the label to edit.</param>
    private async Task EditAsync(string id)
    {
        await InvokeAsync(() => NavigationManager.NavigateTo($"labels/{id}"));
    }

    /// <summary>
    /// Reloads the table data.
    /// </summary>
    private void Reload()
    {
        _table.ReloadServerData();
    }

    /// <summary>
    /// Handles the edit button click for a specific label.
    /// </summary>
    /// <param name="definitionId">The ID of the label to edit.</param>
    private async Task OnEditClicked(string definitionId)
    {
        await EditAsync(definitionId);
    }

    /// <summary>
    /// Handles a row click event in the table.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    private async Task OnRowClick(TableRowClickEventArgs<LabelModel> e)
    {
        await EditAsync(e.Item!.Id);
    }

    /// <summary>
    /// Handles the delete button click for a specific label.
    /// </summary>
    /// <param name="model">The label to delete.</param>
    private async Task OnDeleteClicked(LabelModel model)
    {
        var result = await DialogService.ShowMessageBoxAsync("Delete label?", "Are you sure you want to delete this label?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        var id = model.Id;
        var apiClient = await GetApiAsync();
        await apiClient.DeleteAsync(id);
        Reload();
    }

    /// <summary>
    /// Handles the bulk delete button click.
    /// </summary>
    private Task OnBulkDeleteClicked()
    {
        Snackbar.Add("Not implemented", Severity.Error);
        return Task.CompletedTask;
    }
}
