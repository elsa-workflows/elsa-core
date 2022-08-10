using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Elsa.Multitenancy;
using Elsa.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Testing.Shared.Helpers
{
    /// <summary>
    /// A helper class which 'builds' an <see cref="IHostBuilder"/> for testing purposes.
    /// The reason this is useful is that it also allows building-up callbacks to occur inside
    /// the <see cref="ElsaServiceCollectionExtensions.AddElsa"/> callback (which cannot be
    /// done with a normal host builder alone).
    /// </summary>
    public class ElsaHostBuilderBuilder
    {
        /// <summary>
        /// Gets a collection of the callbacks to be executed upon the <see cref="IServiceCollection"/> of
        /// the host builder as it is created.
        /// </summary>
        /// <returns>The services callbacks</returns>
        public IList<Action<IServiceCollection>> ServiceCallbacks { get; } = new List<Action<IServiceCollection>> {
            services => services.AddLogging(l => l.SetMinimumLevel(LogLevel.Warning)),
        };

        /// <summary>
        /// Gets a collection of the callbacks to be executed upon the <see cref="ContainerBuilder"/> of
        /// the host builder as it is created.
        /// </summary>
        /// <returns>The services callbacks</returns>
        public IList<Action<ContainerBuilder>> ContainerBuilderCallbacks { get; } = new List<Action<ContainerBuilder>>();

        /// <summary>
        /// Gets a collection of the callbacks to be executed upon the <see cref="ElsaOptions"/> of
        /// the host builder's services as it is created.
        /// </summary>
        /// <returns>The Elsa callbacks</returns>
        public IList<Action<ElsaOptionsBuilder>> ElsaCallbacks { get; } = new List<Action<ElsaOptionsBuilder>>();

        Action<ElsaOptionsBuilder> ElsaConfiguration
            => ElsaCallbacks
                .Where(x => !(x is null))
                .Aggregate(EmptyElsaAction, (acc, next) => o => { acc(o); next(o); });

        Action<IServiceCollection> ServiceConfiguration
            => ServiceCallbacks
                .Where(x => !(x is null))
                .Aggregate(EmptyServicesAction, (acc, next) => o => { acc(o); next(o); });

        Action<ContainerBuilder> ContainerBuilderConfiguration
            => ContainerBuilderCallbacks
                .Where(x => !(x is null))
                .Aggregate(EmptyContainerBuilderAction, (acc, next) => o => { acc(o); next(o); });

        static Action<ElsaOptionsBuilder> EmptyElsaAction => o => {};

        static Action<IServiceCollection> EmptyServicesAction => s => {};
        static Action<ContainerBuilder> EmptyContainerBuilderAction => s => { };

        /// <summary>
        /// Gets an <see cref="IHostBuilder"/> configured using the <see cref="ServiceCallbacks"/>
        /// and <see cref="ElsaCallbacks"/>.
        /// </summary>
        /// <returns>A host builder.</returns>
        public IHostBuilder GetHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .UseServiceProviderFactory(new AutofacMultitenantServiceProviderFactory(container => MultitenantContainerFactory.CreateSampleMultitenantContainer(container)))
                .ConfigureServices((hostBuilder, services) => {
                    services.AddElsaServices();
                    ServiceConfiguration(services);
                })
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    var sc = new ServiceCollection();

                    builder.AddElsaServices(sc, ElsaConfiguration);

                    builder.Populate(sc);

                    ContainerBuilderConfiguration(builder);
                });
        }
    }
}