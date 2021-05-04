using Elsa.Client.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ElsaDashboard.Application.Pages.WorkflowDefinitions
{
    partial class Settings
    {
        [Parameter] public WorkflowDefinition WorkflowDefinition { private get; set; } = default!;
        [Parameter] public EventCallback OnChanged { private get; set; }
        private EditContext EditContext { get; set; } = default!;

        protected override void OnInitialized()
        {
            EditContext = new EditContext(WorkflowDefinition);
            EditContext.OnFieldChanged += OnFieldChanged;
        }

        private async void OnFieldChanged(object? sender, FieldChangedEventArgs e) => await OnChanged.InvokeAsync();
    }
}