using ElsaDashboard.Application.Models;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Services
{
    public sealed record ButtonDescriptor
    {
        public ButtonDescriptor(string text, EventCallback<ButtonClickEventArgs>? clickHandler = default, bool isPrimary = false)
        {
            Text = text;
            ClickHandler = clickHandler;
            IsPrimary = isPrimary;
        }
        
        public ButtonDescriptor(string text, EventCallback<ButtonClickEventArgs>? clickHandler) : this(text, clickHandler, false)
        {
        }

        public string Text { get; }
        public bool IsPrimary { get; }
        public EventCallback<ButtonClickEventArgs>? ClickHandler { get; }
    }
    
}