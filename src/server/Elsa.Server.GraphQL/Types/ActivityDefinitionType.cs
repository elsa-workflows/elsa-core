using Elsa.Models;
using HotChocolate.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class ActivityDefinitionType : ObjectType<ActivityDefinition>
    {
        protected override void Configure(IObjectTypeDescriptor<ActivityDefinition> descriptor)
        {
            descriptor.Field(x => x.State).Type<VariablesType>();
        }
    }
}