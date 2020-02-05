using Elsa.Models;
using HotChocolate.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class WorkflowDefinitionVersionType : ObjectType<WorkflowDefinitionVersion>
    {
        protected override void Configure(IObjectTypeDescriptor<WorkflowDefinitionVersion> descriptor)
        {
            descriptor.Field(x => x.Variables).Type<AnyType>();
        }
    }
}