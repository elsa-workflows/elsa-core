using Elsa.Models;
using HotChocolate.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class ActivityDefinitionType : ObjectType<ActivityDefinitionRecord>
    {
        protected override void Configure(IObjectTypeDescriptor<ActivityDefinitionRecord> descriptor)
        {
            //descriptor.Field(x => x.State).Type<VariablesType>();
            //descriptor.Field(x => x.State).Type<MyAnyType>();
            //descriptor.Field(x => x.State).Type<AnyType>();
        }
    }
}