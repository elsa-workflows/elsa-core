using Microsoft.AspNetCore.Components.Routing;

namespace ElsaDashboard.Application.Models
{
    public class MenuItem
    {
        public MenuItem(string text, string url, string icon, NavLinkMatch match = NavLinkMatch.Prefix)
        {
            Text = text;
            Url = url;
            Icon = icon;
        }
        
        public string Text { get; init; }
        public string Icon { get; init; }
        public string Url { get; init; }
        public NavLinkMatch Match { get; init; } = NavLinkMatch.Prefix;
    }
}