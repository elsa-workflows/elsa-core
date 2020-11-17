using System.Collections.Generic;
using ElsaDashboard.Application.Models;
using Microsoft.AspNetCore.Components.Routing;

namespace ElsaDashboard.Application.Shared
{
    partial class NavMenu
    {
        public NavMenu()
        {
            MenuItems = new[]
            {
                new MenuItem("Home", "", "M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6", NavLinkMatch.All),
                new MenuItem("Workflows", "workflows", "M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z", NavLinkMatch.Prefix),
                new MenuItem("Composite Activities", "composite-activities", "M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z", NavLinkMatch.Prefix),
            };
        }

        public IEnumerable<MenuItem> MenuItems { get; set; }
    }
}