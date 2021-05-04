using YesSql;

namespace Elsa.Persistence.YesSql.Services
{
    public interface ISessionProvider
    {
        ISession CreateSession();
    }
}