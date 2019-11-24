using System;
using Elsa;
using Elsa.AutoMapper.Extensions;
using Elsa.Models;
using Elsa.Server.GraphQL;
using Elsa.Server.GraphQL.Mapping;
using GraphQL.NodaTime;
using GraphQL.Server;
using GraphQL.Types;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class GraphQLServiceCollectionExtensions
    {
        public static ElsaBuilder AddGraphQL(this ElsaBuilder builder, Action<GraphQLOptions> configure = null)
        {
            var services = builder.Services;

            services
                .AddScoped<ElsaSchema>()
                .AddSingleton<InstantGraphType>()
                .AddSingleton<EnumerationGraphType<WorkflowStatus>>()
                .AddAutoMapperProfile<GraphQLProfile>(ServiceLifetime.Singleton)
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