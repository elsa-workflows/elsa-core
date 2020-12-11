using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Application.Attributes;
using ElsaDashboard.Application.Extensions;
using ElsaDashboard.Application.Services;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace ElsaDashboard.Application.Pages.WorkflowInstances
{
    partial class List : IDisposable
    {
        [QueryStringParameter] public int Page { get; set; } = 0;
        [QueryStringParameter] public int PageSize { get; set; } = 10;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
        [Inject] private IWorkflowRegistryService WorkflowRegistryService { get; set; } = default!;
        [Inject] private IConfirmDialogService ConfirmDialogService { get; set; } = default!;
        private PagedList<WorkflowInstance> WorkflowInstances { get; set; } = new();
        private IDictionary<(string, int), WorkflowBlueprint> WorkflowBlueprints { get; set; } = new Dictionary<(string, int), WorkflowBlueprint>();

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
        }
        
        public override Task SetParametersAsync(ParameterView parameters)
        {
            this.SetParametersFromQueryString(NavigationManager);
            return base.SetParametersAsync(parameters);
        }

        protected override async Task OnInitializedAsync()
        {
            NavigationManager.LocationChanged += OnLocationChanged;
            
            var workflowBlueprints = await WorkflowRegistryService.ListAsync();
            WorkflowBlueprints = workflowBlueprints.Items.ToDictionary(x => (x.Id, x.Version));
        }

        protected override async Task OnParametersSetAsync()
        {
            await LoadWorkflowInstancesAsync();
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
            WorkflowInstances = await WorkflowInstanceService.ListAsync(Page, PageSize);
        }
        
        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            this.SetParametersFromQueryString(NavigationManager);
            LoadWorkflowInstancesAsync().ContinueWith(_ => InvokeAsync(StateHasChanged));
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