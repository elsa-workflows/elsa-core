using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Pages.WorkflowInstances
{
    partial class List
    {
        [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
        private PagedList<WorkflowInstance> WorkflowInstances { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            WorkflowInstances = await WorkflowInstanceService.ListAsync();
        }
    }
}