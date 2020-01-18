using System;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Config;
using Rebus.ServiceProvider;

namespace Elsa.Messaging
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection WithServiceBus(this ElsaOptions configuration, Func<RebusConfigurer, RebusConfigurer> setup) => configuration.Services.AddRebus(setup);
        public static IServiceCollection WithServiceBus(this ElsaOptions configuration, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> setup) => configuration.Services.AddRebus(setup);
    }
}