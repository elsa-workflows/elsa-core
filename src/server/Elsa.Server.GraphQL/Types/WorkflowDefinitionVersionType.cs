using Elsa.Models;
using HotChocolate.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class WorkflowDefinitionVersionType : ObjectType<WorkflowDefinition>
    {
        protected override void Configure(IObjectTypeDescriptor<WorkflowDefinition> descriptor)
        {
            //descriptor.Field(x => x.Variables).Type<VariablesType>();
        }
    }
}