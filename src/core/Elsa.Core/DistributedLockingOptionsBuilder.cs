using System;
using System.IO;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa
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