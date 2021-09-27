using System.Reflection;
using AutoFixture.Kernel;
using Elsa.Testing.Shared.AutoFixture.SpecimenBuilders;

namespace Elsa.Testing.Shared.AutoFixture.Customizations
{
    public class MockBookmarkProvidersCustomization : SpecimenBuilderForParameterCustomization
    {
        readonly int _howMany;

        protected override ISpecimenBuilder GetUnfilteredSpecimenBuilder()
            => new MockBookmarkProvidersSpecimenBuilder(_howMany);

        public MockBookmarkProvidersCustomization(ParameterInfo parameter, int howMany) : base(parameter)
        {
            _howMany = howMany;
        }
    }
}