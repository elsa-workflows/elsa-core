using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Icons
{
    public abstract class Icon : ComponentBase
    {
        [Parameter] public string CssClass { get; set; } = "mr-3 h-6 w-6 text-gray-500 group-hover:text-gray-500 group-focus:text-gray-600 transition ease-in-out duration-150";

        public static RenderFragment Render<T>(string cssClass) where T : Icon =>
            builder =>
            {
                builder.OpenComponent<T>(0);
                builder.AddAttribute(1,"CssClass", cssClass);
                builder.CloseComponent();
            };
    }
}