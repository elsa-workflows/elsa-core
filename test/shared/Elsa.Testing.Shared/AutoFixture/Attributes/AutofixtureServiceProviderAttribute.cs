using System;
using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;
using AutoFixture.Xunit2;
using Elsa.Testing.Shared.AutoFixture.SpecimenBuilders;

namespace Elsa.Testing.Shared.AutoFixture.Attributes
{
    /// <summary>
    /// Customizes the parameter so that it creates an <see cref="IServiceProvider"/>
    /// using Moq.  That mock service provider gets services by resolving them from Autofixture.
    /// </summary>
    /// <seealso cref="AutofixtureServiceProviderSpecimenBuilder"/>
    public class AutofixtureServiceProviderAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
            => new AutofixtureServiceProviderCustomization(parameter);

        class AutofixtureServiceProviderCustomization : ICustomization
        {
            readonly ParameterInfo parameter;

            public void Customize(IFixture fixture)
            {
                fixture.Customizations.Insert(0, GetSpecimenBuilder());
            }

            ISpecimenBuilder GetSpecimenBuilder()
            {
                var paramSpec = new ParameterSpecification(parameter.ParameterType, parameter.Name);
                var specimenBuilder = new AutofixtureServiceProviderSpecimenBuilder();
                return new FilteringSpecimenBuilder(specimenBuilder, paramSpec);
            }

            public AutofixtureServiceProviderCustomization(ParameterInfo parameter)
            {
                this.parameter = parameter;
            }
        }

    }
}