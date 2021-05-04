using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace ElsaDashboard.Backend.Extensions
{
    public static class GrpcEndpointBuilderExtensions
    {
        private const string SameOriginCorsPolicy = "AllowSameOrigin";

        public static IEndpointRouteBuilder MapGrpcEndpoint<T>(
            this IEndpointRouteBuilder endpoints,
            Action<GrpcServiceEndpointConventionBuilder>? configure = default) where T : class
        {
            var builder = endpoints.MapGrpcService<T>().EnableGrpcWeb().RequireCors(SameOriginCorsPolicy);
            configure?.Invoke(builder);
            return endpoints;
        }
    }
}
