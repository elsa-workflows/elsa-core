using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Runtime;
using YesSql;

namespace Elsa.Persistence.YesSql.StartupTasks
{
    public class StoreInitializationTask : IStartupTask
    {
        private readonly IStore store;

        public StoreInitializationTask(IStore store)
        {
            this.store = store;
        }
        
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await InitializeAsync();
        }

        private Task InitializeAsync()
        {
            // Workaround, see: https://github.com/sebastienros/yessql/issues/188
            var method = typeof(Store).GetMethod("InitializeAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Task) method.Invoke(store, null);
        }
    }
}