using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Shared
{
    partial class ButtonDropdown
    {
        [Parameter] public string Text { get; set; } = "Button";
        [Parameter] public RenderFragment ChildContent { get; set; } = default!;
        [Parameter] public IEnumerable<ButtonDropdownItem> Items {get;set;} = new List<ButtonDropdownItem>();
        [Parameter] public EventCallback<ButtonDropdownItem> ItemSelected { get; set; }

        private async Task OnItemClicked(ButtonDropdownItem item) => await ItemSelected.InvokeAsync(item);
    }

    public record ButtonDropdownItem(string Text, string? Name = default, string? Url = default, bool IsSelected = false)
    {
        public ButtonDropdownItem(string text) : this(text, text)
        {
        }
    }
}