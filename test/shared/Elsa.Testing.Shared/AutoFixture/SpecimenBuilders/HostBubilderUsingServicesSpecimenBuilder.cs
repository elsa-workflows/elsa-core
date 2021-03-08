using System;
using AutoFixture.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Testing.Shared.AutoFixture.SpecimenBuilders
{
    public class HostBubilderUsingServicesSpecimenBuilder : ISpecimenBuilder
    {
        readonly Action<IServiceCollection> serviceConfig;

        public object Create(object request, ISpecimenContext context)
            => request.IsAnAutofixtureRequestForType<IHostBuilder>()? GetHostBuilder() : new NoSpecimen();

        IHostBuilder GetHostBuilder()
            => Host.CreateDefaultBuilder().ConfigureServices((hostBuilder, services) => serviceConfig(services));

        public HostBubilderUsingServicesSpecimenBuilder(Action<IServiceCollection> serviceConfig)
        {
            this.serviceConfig = serviceConfig;
        }
    }
}