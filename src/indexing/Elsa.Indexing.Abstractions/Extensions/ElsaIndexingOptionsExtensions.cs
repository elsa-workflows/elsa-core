using System;
using System.Reflection;
using Elsa.Options;
using MediatR.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Indexing.Extensions
{
    public static class ElsaIndexingOptionsExtensions
    {
        public static ElsaOptionsBuilder UseIndexing(this ElsaOptionsBuilder options, Action<ElsaIndexingOptions> configure)
        {
            var indexingOptions = new ElsaIndexingOptions(options.Services);
            configure.Invoke(indexingOptions);

            var serviceConfiguration = new MediatRServiceConfiguration();
            serviceConfiguration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            ServiceRegistrar.AddMediatRClasses(options.Services, serviceConfiguration);

            return options;
        }
    }
}