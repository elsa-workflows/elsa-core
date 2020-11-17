using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Pages
{
    partial class Workflows
    {
        [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
        private PagedList<WorkflowDefinition> WorkflowDefinitions { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            WorkflowDefinitions = await WorkflowDefinitionService.ListAsync();
        }
    }
}