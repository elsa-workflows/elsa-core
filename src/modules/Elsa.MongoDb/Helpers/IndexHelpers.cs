using MongoDB.Driver;

namespace Elsa.MongoDb.Helpers;

public static class IndexHelpers
{
    public static async Task CreateAsync<T>(IMongoCollection<T> collection, Func<IMongoCollection<T>, IndexKeysDefinitionBuilder<T>, Task> func)
    {
        var builder = Builders<T>.IndexKeys;
        await func(collection, builder);
    }
}