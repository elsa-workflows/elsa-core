using HotChocolate.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class ActivityPropertyOptionsUnionType : UnionType
    {
        protected override void Configure(IUnionTypeDescriptor descriptor)
        {
            descriptor.Name("ActivityPropertyOptions");
            descriptor.Type<SelectOptionsType>();
            descriptor.Type<ExpressionOptionsType>();
        }
    }
}