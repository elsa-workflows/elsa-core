using YesSql;

namespace Elsa.Persistence.YesSql.Services
{
    public class FlushingSessionProvider : ISessionProvider
    {
        private readonly IStore store;

        public FlushingSessionProvider(IStore store)
        {
            this.store = store;
        }
        
        public ISession GetSession()
        {
            return store.CreateSession();
        }
    }
}