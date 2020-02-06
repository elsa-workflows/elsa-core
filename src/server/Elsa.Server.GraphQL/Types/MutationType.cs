using HotChocolate.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class MutationType : ObjectType<Mutation>
    {
        protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
        {
        }
    }
}