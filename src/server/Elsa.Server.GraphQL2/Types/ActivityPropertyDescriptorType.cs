using Elsa.Metadata;
using HotChocolate.Types;

namespace Elsa.Server.GraphQL2.Types
{
    public class ActivityPropertyDescriptorType : ObjectType<ActivityPropertyDescriptor>
    {
        protected override void Configure(IObjectTypeDescriptor<ActivityPropertyDescriptor> descriptor)
        {
            descriptor.Field(x => x.Options).Type<ActivityPropertyOptionsUnionType>();
        }
    }
}