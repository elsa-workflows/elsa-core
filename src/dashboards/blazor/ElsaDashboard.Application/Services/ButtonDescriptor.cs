using System;
using System.Threading.Tasks;
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

        public ButtonDescriptor(string text, Func<ButtonClickEventArgs, Task> clickHandler, bool isPrimary)
        {
            Text = text;
            ClickHandler = new EventCallbackFactory().Create(this, clickHandler);
            IsPrimary = isPrimary;
        }

        public static ButtonDescriptor Create(string text, Func<ButtonClickEventArgs, Task> clickHandler, bool isPrimary = false)
        {
            var descriptor = new ButtonDescriptor(
                text,
                clickHandler,
                isPrimary
            );

            return descriptor;
        }

        public string Text { get; init; }
        public bool IsPrimary { get; init; }
        public EventCallback<ButtonClickEventArgs>? ClickHandler { get; init; }
    }
}