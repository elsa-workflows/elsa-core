using Elsa.Metadata;
using HotChocolate.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class ActivityPropertyDescriptorType : ObjectType<ActivityPropertyDescriptor>
    {
        protected override void Configure(IObjectTypeDescriptor<ActivityPropertyDescriptor> descriptor)
        {
            descriptor.Field(x => x.Options).Type<JObjectType>();
        }
    }
}