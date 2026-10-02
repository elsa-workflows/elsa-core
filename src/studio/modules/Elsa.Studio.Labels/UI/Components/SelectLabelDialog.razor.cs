using Elsa.Studio.Contracts;
using Elsa.Studio.Labels.Client;
using Elsa.Studio.Labels.Comparers;
using Elsa.Studio.Labels.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

using LabelModel = Elsa.Studio.Labels.Models.Label;

namespace Elsa.Studio.Labels.UI.Components;

/// A dialog that selects a Label.
public partial class SelectLabelDialog
{
    private HashSet<LabelModel> _selectedLabels = new(new LabelComparer());

    /// <summary>
    /// The selected labels to edit.
    /// </summary>
    [Parameter] 
#pragma warning disable BL0007
    public IEnumerable<LabelModel> SelectedLabels { 
#pragma warning restore BL0007
        get => _selectedLabels; 
        set => _selectedLabels = new HashSet<LabelModel>(value, new LabelComparer());
    }
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = null!;

    private async Task<ILabelsApi> GetApiAsync()
    {
        return await ApiClientProvider.GetApiAsync<ILabelsApi>();
    }

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

    private Task OnCancelClicked()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    private Task OnSubmitClickedAsync(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
    {
        MudDialog.Close(SelectedLabels);
        return Task.CompletedTask;
    }
}