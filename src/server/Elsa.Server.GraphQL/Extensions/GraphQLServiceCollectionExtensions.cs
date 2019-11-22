using System;
using Elsa;
using Elsa.AutoMapper.Extensions;
using Elsa.Mapping;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Server.GraphQL;
using Elsa.Services;
using GraphQL.Server;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GraphQLServiceCollectionExtensions
    {
        public static ElsaBuilder AddGraphQL(this ElsaBuilder builder, Action<GraphQLOptions> configure = null)
        {
            var services = builder.Services;

            services
                .AddScoped<ElsaSchema>()
                .AddLogging();

            var graphQLOptions = new GraphQLOptions();
            configure?.Invoke(graphQLOptions);
            
            services
                .AddGraphQL(graphQLOptions)
                .AddGraphTypes(ServiceLifetime.Scoped);

            return builder;
        }
    }
}