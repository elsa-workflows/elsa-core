using System;
using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;
using Elsa.Testing.Shared.AutoFixture.SpecimenBuilders;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Testing.Shared.AutoFixture.Customizations
{
    public class HostBubilderUsingServicesCustomization : ICustomization
    {
        readonly Action<IServiceCollection> serviceConfig;
        readonly ParameterInfo? parameter;

        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Insert(0, GetSpecimenBuilder());
        }

        ISpecimenBuilder GetSpecimenBuilder()
        {
            var specimenBuilder = new HostBubilderUsingServicesSpecimenBuilder(serviceConfig);
            if(parameter is null) return specimenBuilder;

            var paramSpec = new ParameterSpecification(parameter.ParameterType, parameter.Name);
            return new FilteringSpecimenBuilder(specimenBuilder, paramSpec);
        }

        public HostBubilderUsingServicesCustomization(Action<IServiceCollection> serviceConfig,
                                                      ParameterInfo? parameter = null)
        {
            this.parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            this.serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));

        }
    }
}