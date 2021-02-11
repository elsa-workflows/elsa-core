using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ElsaDashboard.Application.Icons;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Shared
{
    partial class ButtonDropdown
    {
        [Parameter] public string Text { get; set; } = "Button";
        [Parameter] public Type? IconType { get; set; }
        [Parameter] public ButtonDropdownOrigin Origin { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter] public IEnumerable<ButtonDropdownItem> Items {get;set;} = new List<ButtonDropdownItem>();
        [Parameter] public EventCallback<ButtonDropdownItem> ItemSelected { get; set; }

        private async Task OnItemClicked(ButtonDropdownItem item) => await ItemSelected.InvokeAsync(item);
        private RenderFragment? RenderIcon() => IconType == null ? default : Icon.Render(IconType, "mr-3 h-5 w-5 text-gray-400");

        private string OriginClass => Origin switch
        {
            ButtonDropdownOrigin.TopLeft => "left-0 origin-top-left",
            ButtonDropdownOrigin.TopRight => "right-0 origin-top-right",
            _ => "right-0 origin-top-right"
        };
    }

    public enum ButtonDropdownOrigin
    {
        TopLeft,
        TopRight
    }

    public record ButtonDropdownItem(string Text, string? Name = default, string? Url = default, bool IsSelected = false)
    {
        public ButtonDropdownItem(string text) : this(text, Name: text)
        {
        }
        
        public ButtonDropdownItem(string text, string url) : this(text, Url: url)
        {
        }
    }
}