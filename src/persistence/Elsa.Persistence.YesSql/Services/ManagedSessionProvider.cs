using System;
using YesSql;

namespace Elsa.Persistence.YesSql.Services
{
    public class ManagedSessionProvider : ISessionProvider, IDisposable
    {
        private readonly ISession session;

        public ManagedSessionProvider(ISession session)
        {
            this.session = session;
        }
        
        public ISession GetSession()
        {
            return new ManagedSession(session);
        }

        public void Dispose()
        {
            session.Dispose();
        }
    }
}