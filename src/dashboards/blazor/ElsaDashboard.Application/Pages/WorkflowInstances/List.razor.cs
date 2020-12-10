using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazored.Modal;
using Blazored.Modal.Services;
using Elsa.Client.Models;
using ElsaDashboard.Application.Shared;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Pages.WorkflowInstances
{
    partial class List
    {
        [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
        [Inject] private IWorkflowRegistryService WorkflowRegistryService { get; set; } = default!;
        [Inject] private IModalService ModalService { get; set; } = default!;
        private PagedList<WorkflowInstance> WorkflowInstances { get; set; } = new();
        private IDictionary<(string, int), WorkflowBlueprint> WorkflowBlueprints { get; set; } = new Dictionary<(string, int), WorkflowBlueprint>();

        protected override async Task OnInitializedAsync()
        {
            var workflowBlueprintsTask = WorkflowRegistryService.ListAsync();
            var workflowInstancesTask = WorkflowInstanceService.ListAsync();

            await Task.WhenAll(workflowBlueprintsTask, workflowInstancesTask);

            WorkflowBlueprints = workflowBlueprintsTask.Result.Items.ToDictionary(x => (x.Id, x.Version));
            WorkflowInstances = workflowInstancesTask.Result;
        }

        private async Task OnDeleteWorkflowInstanceClick(WorkflowInstance workflowInstance)
        {
            var options = new ModalOptions { UseCustomLayout = true };
            var parameters = new ModalParameters();
            parameters.Add("Title", "Are you sure?");
            var result = await ModalService.Show<DeleteDialog>("Are you sure?").Result;

            if (result.Cancelled)
                return;
            
            // Delete.
        }

        private static string GetStatusColor(WorkflowStatus status) =>
            status switch
            {
                WorkflowStatus.Idle => "gray",
                WorkflowStatus.Running => "pink",
                WorkflowStatus.Suspended => "blue",
                WorkflowStatus.Finished => "green",
                WorkflowStatus.Faulted => "red",
                WorkflowStatus.Cancelled => "yellow",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
    }
}