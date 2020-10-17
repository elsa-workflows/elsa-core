using Elsa.Activities.Http.Middleware;
using Elsa.Activities.Http.RequestHandlers.Handlers;
using Elsa.Activities.Http.Services;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseHttpActivities(this IApplicationBuilder app)
        {
            return app
                .UseMiddleware<ReceiveHttpRequestMiddleware>()
                .UseRequestHandler<SignalRequestHandler>("/workflows/signal");
        }

        public static IApplicationBuilder UseRequestHandler<THandler>(this IApplicationBuilder app) where THandler : IRequestHandler
        {
            return app.UseMiddleware<RequestHandlerMiddleware<THandler>>();
        }

        public static IApplicationBuilder UseRequestHandler<THandler>(this IApplicationBuilder app, string path) where THandler : IRequestHandler
        {
            return app
                .Map(path, branch => branch.UseMiddleware<RequestHandlerMiddleware<THandler>>());
        }
    }
}