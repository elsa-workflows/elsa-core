using HotChocolate.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class ActivityDefinitionInputType : InputObjectType<ActivityDefinitionInput>
    {
        protected override void Configure(IInputObjectTypeDescriptor<ActivityDefinitionInput> descriptor)
        {
            descriptor.Field(x => x.State).Type<StringType>();
            //descriptor.Field(x => x.State).Type<MyAnyType>();
            //descriptor.Field(x => x.State).Type<AnyType>();
        }
    }
}