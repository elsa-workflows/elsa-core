using System;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Options
{
    public class DistributedLockingOptionsBuilder
    {
        public DistributedLockingOptionsBuilder(ElsaOptionsBuilder elsaOptionsBuilder) => ElsaOptionsBuilder = elsaOptionsBuilder;
        public ElsaOptionsBuilder ElsaOptionsBuilder { get; }
        public IServiceCollection Services => ElsaOptionsBuilder.Services;

        public DistributedLockingOptionsBuilder UseProviderFactory(Func<IServiceProvider, Func<string, IDistributedLock>> factory)
        {
            ElsaOptionsBuilder.ElsaOptions.DistributedLockingOptions.DistributedLockProviderFactory = factory;
            return this;
        }
    }
}