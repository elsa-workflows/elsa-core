using Elsa.Server.GraphQL.Models;
using HotChocolate.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class VersionOptionsInputType : InputObjectType<VersionOptionsInput>
    {
        protected override void Configure(IInputObjectTypeDescriptor<VersionOptionsInput> descriptor)
        {
            descriptor.Field(x => x.Version).Type<IntType>();
        }
    }
}