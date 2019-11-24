using System;
using Elsa.Server.GraphQL.Scalars.Json;
using GraphQL.Types;

namespace Elsa.Server.GraphQL
{
    public class ElsaSchema : Schema
    {
        public ElsaSchema(ElsaQuery query, ElsaMutation mutation, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = query;
            Mutation = mutation;
            
            RegisterValueConverter(new JsonAstValueConverter());
        }
    }
}