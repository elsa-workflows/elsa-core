using Elsa.Server.GraphQL.Models;
using HotChocolate.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class MutationType : ObjectType<Mutation>
    {
        protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
        {
            // descriptor
            //     .Field(x => x.SaveWorkflowDefinition(default, default, default, default))
            //     .Argument("id", x => x.Type<NonNullType<StringType>>())
            //     .Argument("saveAction", x => x.Type<NonNullType<EnumType<WorkflowSaveAction>>>())
            //     .Argument("workflowInput", x => x.Type<WorkflowInputType>());
        }
    }
}