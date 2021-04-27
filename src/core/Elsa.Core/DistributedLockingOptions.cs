using System;
using System.IO;
using Medallion.Threading;
using Medallion.Threading.FileSystem;

namespace Elsa
{
    public class DistributedLockingOptions
    {
        public DistributedLockingOptions()
        {
            DistributedLockProviderFactory = sp => name => new FileDistributedLock(new DirectoryInfo(Path.GetTempPath()), name);
        }

        public Func<IServiceProvider, Func<string, IDistributedLock>> DistributedLockProviderFactory { get; set; }
    }
}