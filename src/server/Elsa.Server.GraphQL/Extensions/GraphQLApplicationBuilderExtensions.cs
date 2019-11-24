using Microsoft.AspNetCore.Builder;

namespace Elsa.Server.GraphQL.Extensions
{
    public static class GraphQLApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseElsaGraphQL(this IApplicationBuilder app)
        {
            return app.UseGraphQL<ElsaSchema>();
        }
    }
}