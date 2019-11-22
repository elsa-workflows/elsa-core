using Elsa.Server.GraphQL;

namespace Microsoft.AspNetCore.Builder
{
    public static class GraphQLApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseElsaGraphQL(this IApplicationBuilder app)
        {
            return app.UseGraphQL<ElsaSchema>();
        }
    }
}