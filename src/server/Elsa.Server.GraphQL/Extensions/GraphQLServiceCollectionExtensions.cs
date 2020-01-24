using System;
using Elsa;
using Elsa.AutoMapper.Extensions;
using Elsa.Models;
using Elsa.Server.GraphQL;
using Elsa.Server.GraphQL.Mapping;
using Elsa.Server.GraphQL.Mutations;
using Elsa.Server.GraphQL.Queries;
using Elsa.Server.GraphQL.Services;
using GraphQL.NodaTime;
using GraphQL.Server;
using GraphQL.Types;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class GraphQLServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQL(this IServiceCollection services, Action<GraphQLOptions> configure = null)
        {
            services
                .AddScoped<ElsaSchema>()
                .AddSingleton<InstantGraphType>()
                .AddSingleton<EnumerationGraphType<WorkflowStatus>>()
                .AddQueryProvider<ListWorkflowDefinitions>()
                .AddQueryProvider<ListWorkflowInstances>()
                .AddQueryProvider<GetWorkflowDefinition>()
                .AddMutationProvider<DefineWorkflow>()
                .AddMutationProvider<UpdateWorkflowDefinition>()
                .AddMutationProvider<RunWorkflow>()
                .AddMutationProvider<PublishWorkflow>()
                .AddMutationProvider<DeleteWorkflowDefinition>()
                .AddAutoMapperProfile<GraphQLProfile>(ServiceLifetime.Singleton)
                .AddLogging();

            var graphQLOptions = new GraphQLOptions();
            configure?.Invoke(graphQLOptions);
            
            services
                .AddGraphQL(graphQLOptions)
                .AddGraphTypes(ServiceLifetime.Scoped);

            return services;
        }

        public static IServiceCollection AddMutationProvider<T>(this IServiceCollection services) where T : class, IMutationProvider => 
            services.AddScoped<IMutationProvider, T>();

        public static IServiceCollection AddQueryProvider<T>(this IServiceCollection services) where T : class, IQueryProvider => 
            services.AddScoped<IQueryProvider, T>();
    }
}