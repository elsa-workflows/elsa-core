using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Pages
{
    partial class WorkflowRegistry
    {
        [Inject] private IWorkflowRegistryService WorkflowRegistryService { get; set; } = default!;
        private PagedList<WorkflowBlueprint> WorkflowBlueprints { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            WorkflowBlueprints = await WorkflowRegistryService.ListAsync();
        }
    }
}