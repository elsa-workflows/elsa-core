using System;
using System.Reflection;
using AutoFixture.Kernel;
using Elsa.Testing.Shared.AutoFixture.SpecimenBuilders;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Testing.Shared.AutoFixture.Customizations
{
    public class HostBuilderUsingServicesCustomization : SpecimenBuilderForParameterCustomization
    {
        readonly Action<IServiceCollection> serviceConfig;

        protected override ISpecimenBuilder GetUnfilteredSpecimenBuilder()
            => new HostBubilderUsingServicesSpecimenBuilder(serviceConfig);

        public HostBuilderUsingServicesCustomization(Action<IServiceCollection> serviceConfig,
                                                      ParameterInfo? parameter) : base(parameter)
        {
            this.serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
        }
    }
}