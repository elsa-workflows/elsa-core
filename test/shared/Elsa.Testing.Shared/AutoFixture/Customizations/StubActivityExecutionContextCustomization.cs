using System.Reflection;
using AutoFixture.Kernel;
using Elsa.Testing.Shared.AutoFixture.SpecimenBuilders;

namespace Elsa.Testing.Shared.AutoFixture.Customizations
{
    public class StubActivityExecutionContextCustomization : SpecimenBuilderForParameterCustomization
    {
        protected override ISpecimenBuilder GetUnfilteredSpecimenBuilder()
            => new StubActivityExecutionContextSpecimenBuilder();

        public StubActivityExecutionContextCustomization(ParameterInfo param) : base(param) {}
    }
}