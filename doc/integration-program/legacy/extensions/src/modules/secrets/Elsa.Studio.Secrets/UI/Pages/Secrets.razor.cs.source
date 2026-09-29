using Elsa.Secrets;
using Elsa.Secrets.BulkActions;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Secrets.Client;
using Elsa.Studio.Secrets.UI.Components;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Secrets.UI.Pages;

[UsedImplicitly]
public partial class Secrets
{
    private MudTable<SecretModel> _table = null!;
    private HashSet<SecretModel> _selectedRows = new();

    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = null!;
    
    private async Task<ISecretsApi> GetApiAsync()
    {
        return await ApiClientProvider.GetApiAsync<ISecretsApi>();
    }
    
    private async Task<TableData<SecretModel>> ServerReload(TableState state, CancellationToken cancellationToken)
    {
        var apiClient = await GetApiAsync();
        var secrets = await apiClient.ListAsync(cancellationToken);

        return new TableData<SecretModel>
        {
            TotalItems = (int)secrets.Count,
            Items = secrets.Items
        };
    }

    private async Task OnCreateClicked()
    {
        var apiClient = await GetApiAsync();
        var uniqueNameResponse = await apiClient.GenerateUniqueNameAsync();
        var name = uniqueNameResponse.Name;

        var parameters = new DialogParameters<CreateSecretDialog>
        {
            { x => x.SecretName, name }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };

        var dialogInstance = await DialogService.ShowAsync<CreateSecretDialog>("New Secret", parameters, options);
        var dialogResult = await dialogInstance.Result;

        if (!dialogResult!.Canceled)
        {
            var inputModel = (SecretInputModel)dialogResult.Data;
            try
            {
                var model = await apiClient.CreateAsync(inputModel);
                //await EditAsync(model.Id);
                Reload();
            }
            catch (Exception e)
            {
                Snackbar.Add(e.Message, Severity.Error);
            }
        }
    }

    private async Task EditAsync(string id)
    {
        await InvokeAsync(() => NavigationManager.NavigateTo($"secrets/{id}"));
    }

    private void Reload()
    {
        _table.ReloadServerData();
    }

    private async Task OnEditClicked(string definitionId)
    {
        await EditAsync(definitionId);
    }

    private async Task OnRowClick(TableRowClickEventArgs<SecretModel> e)
    {
        await EditAsync(e.Item!.Id);
    }

    private async Task OnDeleteClicked(SecretModel model)
    {
        var result = await DialogService.ShowMessageBoxAsync("Delete Secret?", "Are you sure you want to delete this secret?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        var id = model.Id;
        var apiClient = await GetApiAsync();
        await apiClient.DeleteAsync(id);
        Reload();
    }

    private async Task OnBulkDeleteClicked()
    {
        var result = await DialogService.ShowMessageBoxAsync("Delete Selected Secrets?", "Are you sure you want to delete the selected secrets?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        var ids = _selectedRows.Select(x => x.Id).ToList();
        var request = new BulkDeleteRequest { Ids = ids };
        var apiClient = await GetApiAsync();
        await apiClient.BulkDeleteAsync(request);
        Reload();
    }
}