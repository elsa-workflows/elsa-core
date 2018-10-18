using DotNetify;
using Flowsharp.Web.Components.ViewModels;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseFlowsharpComponents(this IApplicationBuilder app)
        {
            return app
                .UseWebSockets()
                .UseSignalR(routes => routes.MapDotNetifyHub())
                .UseDotNetify(options => options.RegisterAssembly(typeof(WorkflowEditor).Assembly));
        }
    }
}