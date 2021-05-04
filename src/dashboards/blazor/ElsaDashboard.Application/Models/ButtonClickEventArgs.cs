using System;
using ElsaDashboard.Application.Services;

namespace ElsaDashboard.Application.Models
{
    public class ButtonClickEventArgs : EventArgs
    {
        public ButtonClickEventArgs(ButtonDescriptor button)
        {
            Button = button;
        }

        public ButtonDescriptor Button { get; }
    }
}