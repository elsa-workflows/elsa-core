using System;
using ElsaDashboard.Components;
using ElsaDashboard.Models;
using Microsoft.AspNetCore.Components.Rendering;

namespace ElsaDashboard.Extensions
{
    public static class RenderTreeBuilderExtensions
    {
        public static RenderTreeBuilder AddActivityDisplayComponent<T>(this RenderTreeBuilder builder, ActivityDisplayContext context) where T : ActivityDisplayComponent => AddActivityDisplayComponent(builder, typeof(T), context);

        public static RenderTreeBuilder AddActivityDisplayComponent(this RenderTreeBuilder builder, Type type, ActivityDisplayContext context)
        {
            builder.OpenComponent(0, type);
            builder.AddAttribute(1, "DisplayContext", context);
            builder.CloseComponent();
            return builder;
        }
    }
}