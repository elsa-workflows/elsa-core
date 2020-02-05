using Elsa.Server.GraphQL.Models;
using HotChocolate.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class WorkflowInputType : InputObjectType<WorkflowInput>
    {
        protected override void Configure(IInputObjectTypeDescriptor<WorkflowInput> descriptor)
        {
        }
    }
}