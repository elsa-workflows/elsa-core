using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Application.Services;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Pages.WorkflowInstances
{
    partial class List
    {
        [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
        [Inject] private IWorkflowRegistryService WorkflowRegistryService { get; set; } = default!;
        [Inject] private IConfirmDialogService ConfirmDialogService { get; set; } = default!;
        private PagedList<WorkflowInstance> WorkflowInstances { get; set; } = new();
        private IDictionary<(string, int), WorkflowBlueprint> WorkflowBlueprints { get; set; } = new Dictionary<(string, int), WorkflowBlueprint>();

        protected override async Task OnInitializedAsync()
        {
            var workflowBlueprintsTask = WorkflowRegistryService.ListAsync();
            var workflowInstancesTask = LoadWorkflowInstancesAsync();

            await Task.WhenAll(workflowBlueprintsTask, workflowInstancesTask);

            WorkflowBlueprints = workflowBlueprintsTask.Result.Items.ToDictionary(x => (x.Id, x.Version));
        }

        private async Task OnDeleteWorkflowInstanceClick(WorkflowInstance workflowInstance)
        {
            var result = await ConfirmDialogService.Show("Delete Workflow Instance", "Are you sure you want to delete this workflow instance?", "Delete");

            if (result.Cancelled)
                return;

            await WorkflowInstanceService.DeleteAsync(workflowInstance.WorkflowInstanceId);
            await LoadWorkflowInstancesAsync();
        }

        private async Task LoadWorkflowInstancesAsync()
        {
            WorkflowInstances = await WorkflowInstanceService.ListAsync();
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