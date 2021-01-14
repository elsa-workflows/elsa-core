using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Application.Attributes;
using ElsaDashboard.Application.Extensions;
using ElsaDashboard.Application.Services;
using ElsaDashboard.Application.Shared;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;

namespace ElsaDashboard.Application.Pages.WorkflowInstances
{
    partial class List : IDisposable
    {
        [QueryStringParameter] public int Page { get; set; } = 0;
        [QueryStringParameter] public int PageSize { get; set; } = 15;
        [QueryStringParameter("workflow")] public string? SelectedWorkflowId { get; set; }
        [QueryStringParameter("status")] public WorkflowStatus? SelectedWorkflowStatus { get; set; }
        [QueryStringParameter("orderBy")] public OrderBy? SelectedOrderBy { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
        [Inject] private IWorkflowRegistryService WorkflowRegistryService { get; set; } = default!;
        [Inject] private IConfirmDialogService ConfirmDialogService { get; set; } = default!;
        private PagedList<WorkflowInstanceSummary> WorkflowInstances { get; set; } = new();
        private IDictionary<(string, int), WorkflowBlueprintSummary> WorkflowBlueprints { get; set; } = new Dictionary<(string, int), WorkflowBlueprintSummary>();
        private IEnumerable<WorkflowBlueprintSummary> LatestWorkflowBlueprints => GetLatestVersions(WorkflowBlueprints.Values);
        private SearchModel SearchModel { get; set; } = new();
        private EditContext EditContext { get; set; } = default!;
        private Stopwatch Stopwatch { get; set; } = new();

        private IEnumerable<ButtonDropdownItem> WorkflowFilterItems =>
            LatestWorkflowBlueprints.Select(x => new ButtonDropdownItem(x.DisplayName!, x.Id, BuildFilterUrl(x.Id, SelectedWorkflowStatus, SelectedOrderBy), x.Id == SelectedWorkflowId))
                .Prepend(new ButtonDropdownItem("All", null, BuildFilterUrl(null, SelectedWorkflowStatus, SelectedOrderBy), SelectedWorkflowId == null));

        private IEnumerable<ButtonDropdownItem> WorkflowStatusFilterItems =>
            new[]
            {
                default(WorkflowStatus?),
                WorkflowStatus.Running,
                WorkflowStatus.Suspended,
                WorkflowStatus.Finished,
                WorkflowStatus.Faulted,
                WorkflowStatus.Cancelled
            }.Select(x => new ButtonDropdownItem(x?.ToString() ?? "All", x.ToString(), BuildFilterUrl(SelectedWorkflowId, x, SelectedOrderBy), x == SelectedWorkflowStatus));

        private IEnumerable<ButtonDropdownItem> OrderByItems =>
            new[] { OrderBy.Finished, OrderBy.LastExecuted, OrderBy.Started }.Select(x => new ButtonDropdownItem(x.ToString(), x.ToString(), BuildFilterUrl(SelectedWorkflowId, SelectedWorkflowStatus, x), x == SelectedOrderBy));

        private string SelectedWorkflowText => SelectedWorkflowId == null ? "Workflow" : LatestWorkflowBlueprints.FirstOrDefault(x => x.Id == SelectedWorkflowId)?.DisplayName ?? "Workflow";
        private string SelectedWorkflowStatusText => SelectedWorkflowStatus?.ToString() ?? "Status";
        private string SelectedOrderByText => SelectedOrderBy == null ? "Sort" : $"Sort By {SelectedOrderBy}";

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            this.SetParametersFromQueryString(NavigationManager);
            await base.SetParametersAsync(parameters);
        }

        protected override async Task OnInitializedAsync()
        {
            NavigationManager.LocationChanged += OnLocationChanged;
            EditContext = new EditContext(SearchModel);
            
            var workflowBlueprints = await WorkflowRegistryService.ListAsync();
            WorkflowBlueprints = workflowBlueprints.Items.ToDictionary(x => (x.Id, x.Version));
        }

        protected override async Task OnParametersSetAsync()
        {
            await LoadWorkflowInstancesAsync();
        }

        private async Task LoadWorkflowInstancesAsync()
        {
            Stopwatch.Restart();
            SetDefaults();
            WorkflowInstances = await WorkflowInstanceService.ListAsync(Page, PageSize, SelectedWorkflowId, SelectedWorkflowStatus, SelectedOrderBy, SearchModel.SearchTerm);
            Stopwatch.Stop();
            Console.WriteLine(Stopwatch.Elapsed.ToString());
        }

        private static string BuildFilterUrl(string? workflowId, WorkflowStatus? workflowStatus, OrderBy? orderBy)
        {
            var workflowStatusText = workflowStatus?.ToString();
            var orderByText = orderBy?.ToString();

            var query = new Dictionary<string, string?>
                {
                    ["workflow"] = workflowId,
                    ["status"] = workflowStatusText,
                    ["orderBy"] = orderByText
                }
                .Where(x => x.Value != null)
                .ToDictionary(x => x.Key, x => x.Value!);

            return QueryHelpers.AddQueryString("workflow-instances", query);
        }

        private async Task OnDeleteWorkflowInstanceClick(WorkflowInstanceSummary workflowInstance)
        {
            var result = await ConfirmDialogService.Show("Delete Workflow Instance", "Are you sure you want to delete this workflow instance?", "Delete");

            if (result.Cancelled)
                return;

            await WorkflowInstanceService.DeleteAsync(workflowInstance.Id);
            await LoadWorkflowInstancesAsync();
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            this.SetParametersFromQueryString(NavigationManager);
            LoadWorkflowInstancesAsync().ContinueWith(_ => InvokeAsync(StateHasChanged));
        }

        private async Task OnSearchSubmit() => await LoadWorkflowInstancesAsync();

        private void SetDefaults()
        {
            if (PageSize == 0)
                PageSize = 15;
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

        private static IEnumerable<WorkflowBlueprintSummary> GetLatestVersions(IEnumerable<WorkflowBlueprintSummary> workflowBlueprints) => workflowBlueprints.GroupBy(x => x.Id).Select(x => x.OrderByDescending(y => y.Version).First());
    }
}