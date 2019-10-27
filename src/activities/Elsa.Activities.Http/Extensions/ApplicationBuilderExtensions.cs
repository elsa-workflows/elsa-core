using Elsa.Activities.Http.Middleware;
using Elsa.Activities.Http.RequestHandlers.Handlers;
using Elsa.Activities.Http.Services;
using Microsoft.AspNetCore.Builder;

namespace Elsa.Activities.Http.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseHttpActivities(this IApplicationBuilder app)
        {
            return app
                .UseRequestHandler<TriggerRequestHandler>()
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