using Elsa.Agents;
using Elsa.Studio.Agents.Client;
using Elsa.Studio.Agents.UI.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Agents.UI.Pages;

[UsedImplicitly]
public partial class Agents
{
    private MudTable<AgentModel> _table = null!;
    private HashSet<AgentModel> _selectedRows = new();

    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = null!;
    [Inject] private IFiles Files { get; set; } = null!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = null!;

    private async Task<IAgentsApi> GetAgentsApiAsync()
    {
        return await ApiClientProvider.GetApiAsync<IAgentsApi>();
    }
    
    private async Task<TableData<AgentModel>> ServerReload(TableState state, CancellationToken cancellationToken)
    {
        var apiClient = await GetAgentsApiAsync();
        var agents = await apiClient.ListAsync(cancellationToken);

        return new TableData<AgentModel>
        {
            TotalItems = (int)agents.Count,
            Items = agents.Items
        };
    }

    private async Task OnCreateAgentClicked()
    {
        var apiClient = await GetAgentsApiAsync();
        var agentNameResponse = await apiClient.GenerateUniqueNameAsync();
        var agentName = agentNameResponse.Name;

        var parameters = new DialogParameters<CreateAgentDialog>
        {
            { x => x.AgentName, agentName }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };

        var dialogInstance = await DialogService.ShowAsync<CreateAgentDialog>("New Agent", parameters, options);
        var dialogResult = await dialogInstance.Result;

        if (!dialogResult!.Canceled)
        {
            var agentInputModel = (AgentInputModel)dialogResult.Data;
            try
            {
                var agent = await apiClient.CreateAsync(agentInputModel);
                await EditAsync(agent.Id);
            }
            catch (Exception e)
            {
                Snackbar.Add(e.Message, Severity.Error);
            }
        }
    }

    private async Task EditAsync(string id)
    {
        await InvokeAsync(() => NavigationManager.NavigateTo($"ai/agents/{id}"));
    }

    private void Reload()
    {
        _table.ReloadServerData();
    }

    private async Task OnEditClicked(string definitionId)
    {
        await EditAsync(definitionId);
    }

    private async Task OnRowClick(TableRowClickEventArgs<AgentModel> e)
    {
        await EditAsync(e.Item!.Id);
    }

    private async Task OnDeleteClicked(AgentModel agent)
    {
        var result = await DialogService.ShowMessageBoxAsync("Delete Agent?", "Are you sure you want to delete this agent?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        var agentId = agent.Id;
        var apiClient = await GetAgentsApiAsync();
        await apiClient.DeleteAsync(agentId);
        ActivityRegistry.MarkStale();
        ActivityDisplaySettingsRegistry.MarkStale();
        Reload();
    }

    private async Task OnBulkDeleteClicked()
    {
        var result = await DialogService.ShowMessageBoxAsync("Delete Selected Agents?", $"Are you sure you want to delete the selected agents?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        var ids = _selectedRows.Select(x => x.Id).ToList();
        var request = new BulkDeleteRequest { Ids = ids };
        var apiClient = await GetAgentsApiAsync();
        await apiClient.BulkDeleteAsync(request);
        ActivityRegistry.MarkStale();
        ActivityDisplaySettingsRegistry.MarkStale();
        Reload();
    }
}