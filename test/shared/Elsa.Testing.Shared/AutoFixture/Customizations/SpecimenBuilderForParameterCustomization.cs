using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;

namespace Elsa.Testing.Shared.AutoFixture.Customizations
{
    /// <summary>
    /// Helper base class for creating an Autofixture customization which customizes
    /// a single parameter value with a specified specimen builder.
    /// </summary>
    public abstract class SpecimenBuilderForParameterCustomization : ICustomization
    {
        readonly ParameterInfo? parameter;

        public virtual void Customize(IFixture fixture)
        {
            fixture.Customizations.Insert(0, GetSpecimenBuilder());
        }

        protected virtual ISpecimenBuilder GetSpecimenBuilder()
        {
            var unfilteredSpecimenBuilder = GetUnfilteredSpecimenBuilder();
            if(parameter is null) return unfilteredSpecimenBuilder;

            var paramSpec = new ParameterSpecification(parameter.ParameterType, parameter.Name);
            return new FilteringSpecimenBuilder(unfilteredSpecimenBuilder, paramSpec);
        }

        protected abstract ISpecimenBuilder GetUnfilteredSpecimenBuilder();

        public SpecimenBuilderForParameterCustomization(ParameterInfo? parameter)
        {
            this.parameter = parameter;
        }
    }
}