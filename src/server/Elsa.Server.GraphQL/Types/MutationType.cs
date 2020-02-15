using HotChocolate.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class MutationType : ObjectType<Mutation>
    {
        protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
        {
            descriptor
                .Field(x => x.SaveWorkflowDefinition(default, default, default, default, default, default, default, default))
                .Argument("id", x => x.Type<IdType>());

            descriptor
                .Field(x => x.DeleteWorkflowDefinition(default, default, default))
                .Argument("id", x => x.Type<IdType>());
        }
    }
}