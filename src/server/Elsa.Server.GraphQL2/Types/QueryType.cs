using Elsa.Server.GraphQL2.Queries;
using HotChocolate.Types;

namespace Elsa.Server.GraphQL2.Types
{
    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
        }
    }
}