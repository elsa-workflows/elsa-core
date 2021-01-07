using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Pages.WorkflowRegistry
{
    partial class List
    {
        [Inject] private IWorkflowRegistryService WorkflowRegistryService { get; set; } = default!;
        private PagedList<WorkflowBlueprintSummary> WorkflowBlueprints { get; set; } = new();
        private IEnumerable<IGrouping<string, WorkflowBlueprintSummary>> VersionedWorkflowBlueprints => WorkflowBlueprints.Items.GroupBy(x => x.Id);

        protected override async Task OnInitializedAsync()
        {
            WorkflowBlueprints = await WorkflowRegistryService.ListAsync();
        }
    }
}