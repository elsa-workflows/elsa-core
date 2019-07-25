using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Services
{
    public class ManagedSession : ISession
    {
        private readonly ISession decoratedSession;

        public ManagedSession(ISession decoratedSession)
        {
            this.decoratedSession = decoratedSession;
        }
        
        public void Dispose()
        {
            // Do nothing. Session will be disposed when container is disposed.
        }

        public void Save(object obj) => decoratedSession.Save(obj);
        public void Delete(object item) => decoratedSession.Delete(item);
        public bool Import(object item, int id) => decoratedSession.Import(item, id);
        public Task<IEnumerable<T>> GetAsync<T>(int[] ids) where T : class => decoratedSession.GetAsync<T>(ids);
        public IQuery Query() => decoratedSession.Query();
        public IQuery<T> ExecuteQuery<T>(ICompiledQuery<T> compiledQuery) where T : class => decoratedSession.ExecuteQuery<T>(compiledQuery);
        public void Cancel() => decoratedSession.Cancel();
        public Task FlushAsync() => decoratedSession.FlushAsync();
        public Task CommitAsync() => decoratedSession.CommitAsync();
        public Task<DbTransaction> DemandAsync() => decoratedSession.DemandAsync();
        public ISession RegisterIndexes(params IIndexProvider[] indexProviders) => decoratedSession.RegisterIndexes();
        public IStore Store => decoratedSession.Store;
    }
}