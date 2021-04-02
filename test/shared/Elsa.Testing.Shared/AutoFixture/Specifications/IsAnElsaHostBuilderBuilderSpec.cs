using AutoFixture.Kernel;
using Elsa.Testing.Shared.Helpers;

namespace Elsa.Testing.Shared.AutoFixture.Specifications
{
    public class IsAnElsaHostBuilderBuilderSpec : IRequestSpecification
    {
        public bool IsSatisfiedBy(object request) => request is ElsaHostBuilderBuilder;
    }
}