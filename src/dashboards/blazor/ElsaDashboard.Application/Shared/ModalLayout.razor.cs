using System.Threading.Tasks;
using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Shared
{
    partial class ModalLayout
    {
        [CascadingParameter] private BlazoredModalInstance BlazoredModal { get; set; } = default!;
        [Parameter] public RenderFragment? Icon { get; set; }
        [Parameter] public RenderFragment? Buttons { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public string? AccentColor { get; set; } = "blue";
        public string Title => BlazoredModal.Title;

        private async Task Close() => await BlazoredModal.CloseAsync(ModalResult.Ok(true));
        private async Task Cancel() => await BlazoredModal.CancelAsync();
    }
}