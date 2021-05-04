using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace ElsaDashboard.Application.Models
{
    public class MenuItem
    {
        public MenuItem(string text, string url, RenderFragment icon, NavLinkMatch match = NavLinkMatch.Prefix)
        {
            Text = text;
            Url = url;
            Icon = icon;
        }
        
        public string Text { get; init; }
        public RenderFragment Icon { get; init; }
        public string Url { get; init; }
        public NavLinkMatch Match { get; init; } = NavLinkMatch.Prefix;
    }
}