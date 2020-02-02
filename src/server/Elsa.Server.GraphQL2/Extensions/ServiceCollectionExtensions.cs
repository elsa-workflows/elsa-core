using Elsa.Server.GraphQL2.Queries;
using Elsa.Server.GraphQL2.Types;
using HotChocolate;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Server.GraphQL2.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaGraphQL(this IServiceCollection services)
        {
            return services.AddGraphQL(sp => SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<Query>()
                .AddType<ActivityDescriptorType>()
                .Create());
        }
    }
}