using Elsa.Models;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Services
{
    public interface IElsaMongoDbContext
    {
        IMongoCollection<Bookmark> Bookmarks { get; }
        IMongoCollection<Trigger> Triggers { get; }
        IMongoCollection<WorkflowDefinition> WorkflowDefinitions { get; }
        IMongoCollection<WorkflowExecutionLogRecord> WorkflowExecutionLog { get; }
        IMongoCollection<WorkflowInstance> WorkflowInstances { get; }
    }
}