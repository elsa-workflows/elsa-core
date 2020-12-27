using ElsaDashboard.Application.Icons;
using ElsaDashboard.Application.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ElsaDashboard.Application.Shared
{
    partial class Activity
    {
        public const string DefaultIconClass = "h-10 w-10 text-blue-500";
        [Parameter] public ActivityModel Model { get; set; } = new();
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
        [Parameter] public EventCallback OnEditClick { get; set; }
        [Parameter] public EventCallback OnDeleteClick { get; set; }
        [Parameter] public RenderFragment Body { get; set; } = default!;
        [Parameter] public RenderFragment Icon { get; set; } = default!;

        protected override void OnInitialized()
        {
            if (Body == null!)
                Body = GetDefaultBody();

            if (Icon == null!)
                Icon = GetDefaultIcon();
        }

        private RenderFragment GetDefaultBody() =>
            builder =>
            {
                builder.OpenElement(0, "p");
                builder.AddContent(1, Model.Description);
                builder.CloseElement();
            };
        
        private RenderFragment GetDefaultIcon() =>
            builder =>
            {
                builder.OpenComponent<ActivityIcon>(0);
                builder.AddAttribute(1, "CssClass", DefaultIconClass);
                builder.CloseComponent();
            };

        private string DisplayName => string.IsNullOrWhiteSpace(Model.DisplayName) ? Model.Type : Model.DisplayName;
    }
}