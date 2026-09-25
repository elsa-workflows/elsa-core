namespace Elsa.Server.Web;

public enum PersistenceProvider
{
    Memory,
    EntityFrameworkCore,
    MongoDb,
    Dapper
}