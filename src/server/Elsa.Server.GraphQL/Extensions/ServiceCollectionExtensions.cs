using Elsa.Extensions;
using Elsa.Metadata;
using Elsa.Server.GraphQL.Mapping;
using Elsa.Server.GraphQL.Types;
using Elsa.Services;
using HotChocolate;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Server.GraphQL.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaGraphQL(this IServiceCollection services)
        {
            return services
                .AddScoped<IWorkflowPublisher, WorkflowPublisher>()
                .AddSingleton<VersionOptionsConverter>()
                .AddSingleton<ActivityStateResolver>()
                .AddSingleton<IActivityDescriber, ActivityDescriber>()
                .AddAutoMapperProfile<MappingProfile>(ServiceLifetime.Singleton)
                .AddGraphQL(sp => SchemaBuilder.New()
                    .AddServices(sp)
                    .AddQueryType<QueryType>()
                    .AddMutationType<MutationType>()
                    .AddType<VariableType>()
                    .AddType<ActivityDescriptorType>()
                    .AddType<ActivityPropertyDescriptorType>()
                    .AddType<WorkflowInputType>()
                    .AddType<ActivityDefinitionType>()
                    .AddType<ActivityDefinitionInputType>()
                    .AddType<ConnectionDefinitionType>()
                    .AddType<WorkflowDefinitionVersionType>()
                    .AddType<VersionOptionsInputType>()
                    .AddType<InstantType>()
                    .Create());
        }
    }
}