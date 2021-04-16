using System.Collections.Generic;
using System.Linq;
using Elsa.Persistence.YesSql.Data;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Services
{
    public class SessionProvider : ISessionProvider
    {
        private readonly IStore _store;
        private readonly IIndexProvider[] _indexProviders;

        public SessionProvider(IStore store, IEnumerable<IScopedIndexProvider> indexProviders)
        {
            _store = store;
            _indexProviders = indexProviders.Cast<IIndexProvider>().ToArray();
        }
        
        public ISession CreateSession()
        {
            var session = _store.CreateSession();
            session.RegisterIndexes(_indexProviders);
            return session;
        }
    }
}