using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Pages.WorkflowDefinitions
{
    partial class List
    {
        [Parameter] public int Page { get; set; } = 0;
        [Parameter] public int PageSize { get; set; } = 10;
        [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
        private PagedList<WorkflowDefinition> Definitions { get; set; } = new();

        protected override async Task OnParametersSetAsync()
        {
            Definitions = await WorkflowDefinitionService.ListAsync(Page, PageSize);
        }
    }
}