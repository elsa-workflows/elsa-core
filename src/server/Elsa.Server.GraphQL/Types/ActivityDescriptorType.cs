using Elsa.Metadata;
using HotChocolate.Types;

namespace Elsa.Server.GraphQL2.Types
{
    public class ActivityDescriptorType : ObjectType<ActivityDescriptor>
    {
        protected override void Configure(IObjectTypeDescriptor<ActivityDescriptor> descriptor)
        {
            descriptor.Field(x => x.Type).Type<NonNullType<StringType>>();
        }
    }
}