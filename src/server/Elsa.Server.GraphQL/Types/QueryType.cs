using HotChocolate.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
        }
    }
}