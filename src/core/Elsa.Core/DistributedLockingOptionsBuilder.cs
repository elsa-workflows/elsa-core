using System;
using System.IO;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa
{
    public class DistributedLockingOptionsBuilder
    {
        public DistributedLockingOptionsBuilder(ElsaOptionsBuilder elsaOptions)
        {
            ElsaOptions = elsaOptions;
            DistributedLockProviderFactory = sp => name => new FileDistributedLock(new DirectoryInfo(Environment.CurrentDirectory), name);
        }

        public ElsaOptionsBuilder ElsaOptions { get; }
        public IServiceCollection Services => ElsaOptions.Services;

        public Func<IServiceProvider, Func<string, IDistributedLock>> DistributedLockProviderFactory { get; set; }
    }
}