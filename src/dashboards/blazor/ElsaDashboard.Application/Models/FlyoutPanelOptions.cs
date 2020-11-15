using System;
using System.Collections.Generic;
using ElsaDashboard.Application.Services;
using ElsaDashboard.Application.Shared;

namespace ElsaDashboard.Application.Models
{
    public class FlyoutPanelOptions
    {
        public Type ContentComponentType { get; set; } = typeof(FlyoutPanelPlaceholder);
        public ICollection<ButtonDescriptor> Buttons { get; set; } = new List<ButtonDescriptor>();
        public string? Title { get; set; }
        public IDictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();
    }
}