using System.Reflection;
using AutoFixture.Kernel;
using Elsa.Testing.Shared.AutoFixture.SpecimenBuilders;

namespace Elsa.Testing.Shared.AutoFixture.Customizations
{
    public class StubElsaContextCustomization : SpecimenBuilderForParameterCustomization
    {
        protected override ISpecimenBuilder GetUnfilteredSpecimenBuilder()
            => new StubElsaContextSpecimenBuilder();

        public StubElsaContextCustomization(ParameterInfo? parameter) : base(parameter) {}
    }
}