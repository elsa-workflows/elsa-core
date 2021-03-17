using System.Reflection;
using AutoFixture.Kernel;
using Elsa.Testing.Shared.AutoFixture.SpecimenBuilders;

namespace Elsa.Testing.Shared.AutoFixture.Customizations
{
    public class MockBookmarkProvidersCustomization : SpecimenBuilderForParameterCustomization
    {
        readonly int howMany;

        protected override ISpecimenBuilder GetUnfilteredSpecimenBuilder()
            => new MockBookmarkProvidersSpecimenBuilder(howMany);

        public MockBookmarkProvidersCustomization(ParameterInfo parameter, int howMany) : base(parameter)
        {
            this.howMany = howMany;
        }
    }
}