using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Application.Services;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Pages.WorkflowDefinitions
{
    partial class List
    {
        [Parameter] public int Page { get; set; } = 0;
        [Parameter] public int PageSize { get; set; } = 10;
        [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
        [Inject] private IConfirmDialogService ConfirmDialogService { get; set; } = default!;
        private PagedList<WorkflowDefinition> Definitions { get; set; } = new();

        protected override async Task OnParametersSetAsync()
        {
            await LoadWorkflowDefinitionsAsync();
        }

        private async Task LoadWorkflowDefinitionsAsync()
        {
            Definitions = await WorkflowDefinitionService.ListAsync(Page, PageSize);
        }
        
        private async Task OnDeleteWorkflowDefinitionClick(WorkflowDefinition workflowDefinition)
        {
            var result = await ConfirmDialogService.Show("Delete Workflow Definition", "Are you sure you want to delete this workflow definition? This will also delete any and all of its workflow instances.", "Delete");

            if (result.Cancelled)
                return;

            await WorkflowDefinitionService.DeleteAsync(workflowDefinition.Id);
            await LoadWorkflowDefinitionsAsync();
        }
    }
}