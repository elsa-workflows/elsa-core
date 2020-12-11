﻿using System.Collections.Generic;
using ElsaDashboard.Application.Icons;
using ElsaDashboard.Application.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace ElsaDashboard.Application.Shared
{
    partial class NavMenu
    {
        public NavMenu()
        {
            MenuItems = new[]
            {
                new MenuItem("Home", "", GetIcon<Home>(), NavLinkMatch.All),
                new MenuItem("Workflow Registry", "workflow-registry", GetIcon<Squares>(), NavLinkMatch.Prefix),
                new MenuItem("Workflow Instances", "workflow-instances", GetIcon<PlayCircle>(), NavLinkMatch.Prefix),
                new MenuItem("Workflow Definitions", "workflow-definitions", GetIcon<Database>(), NavLinkMatch.Prefix),
                new MenuItem("Composite Activities", "composite-activities", GetIcon<Icons.Activity>(), NavLinkMatch.Prefix),
            };
        }

        public IEnumerable<MenuItem> MenuItems { get; set; }

        private RenderFragment GetIcon<T>() where T : Icon => Icon.Render<T>("mr-3 h-6 w-6 text-gray-500 group-hover:text-gray-500 group-focus:text-gray-600 transition ease-in-out duration-150");
    }
}