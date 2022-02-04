using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.MultiTenancy;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Services
{
    public class DatabaseInitializer : IStartupTask
    {
        private readonly ITenantStore _tenantStore;
        private readonly IServiceScopeFactory _scopeFactory;

        public DatabaseInitializer(ITenantStore tenantStore, IServiceScopeFactory scopeFactory)
        {
            _tenantStore = tenantStore;
            _scopeFactory = scopeFactory;
        }

        public int Order => 0;

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            foreach (var tenant in _tenantStore.GetTenants())
            {
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);

                var dbContextProvider = scope.ServiceProvider.GetRequiredService<ElsaMongoDbContextProvider>();

                await CreateWorkflowInstancesIndexes(dbContextProvider, cancellationToken);
                await CreateWorkflowDefinitionsIndexes(dbContextProvider, cancellationToken);
                await CreateWorkflowExecutionLogIndexes(dbContextProvider, cancellationToken);
                await CreateBookmarkIndexes(dbContextProvider, cancellationToken);
            }
        }

        private async Task CreateWorkflowInstancesIndexes(ElsaMongoDbContextProvider dbContextProvider, CancellationToken cancellationToken)
        {
            var builder = Builders<WorkflowInstance>.IndexKeys;
            var tenantKeysDefinition = builder.Ascending(x => x.TenantId);
            var versionKeysDefinition = builder.Ascending(x => x.Version);
            var workflowStatusKeysDefinition = builder.Ascending(x => x.WorkflowStatus);
            var workflowNameKeysDefinition = builder.Ascending(x => x.Name);
            var contextIdKeysDefinition = builder.Ascending(x => x.ContextId);
            var contextTypeKeysDefinition = builder.Ascending(x => x.ContextType);
            var correlationIdKeysDefinition = builder.Ascending(x => x.CorrelationId);
            var createdAtKeysDefinition = builder.Ascending(x => x.CreatedAt);
            var definitionKeysDefinition = builder.Ascending(x => x.DefinitionId);
            var definitionVersionIdKeysDefinition = builder.Ascending(x => x.DefinitionVersionId);
            var faultedAtKeysDefinition = builder.Ascending(x => x.FaultedAt);
            var finishedAtKeysDefinition = builder.Ascending(x => x.FinishedAt);
            var lastExecutedAtKeysDefinition = builder.Ascending(x => x.LastExecutedAt);
            var workflowStatusDefinitionVersionKeysDefinition = builder.Ascending(x => x.WorkflowStatus).Ascending(x => x.DefinitionId).Ascending(x => x.Version);
            var workflowStatusDefinitionKeysDefinition = builder.Ascending(x => x.WorkflowStatus).Ascending(x => x.DefinitionId);
            var collection = dbContextProvider.WorkflowInstances;

            await CreateIndexesAsync(
                collection,
                cancellationToken,
                tenantKeysDefinition,
                versionKeysDefinition,
                workflowStatusKeysDefinition,
                workflowNameKeysDefinition,
                contextIdKeysDefinition,
                contextTypeKeysDefinition,
                correlationIdKeysDefinition,
                createdAtKeysDefinition,
                definitionKeysDefinition,
                definitionVersionIdKeysDefinition,
                faultedAtKeysDefinition,
                finishedAtKeysDefinition,
                lastExecutedAtKeysDefinition,
                workflowStatusDefinitionVersionKeysDefinition,
                workflowStatusDefinitionKeysDefinition);

            var workflowDefinitionIdAndVersionAndStatusKeyDefinition = builder.Combine(builder.Ascending(x => x.DefinitionId), builder.Ascending(x => x.Version), builder.Ascending(x => x.WorkflowStatus));
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<WorkflowInstance>(workflowDefinitionIdAndVersionAndStatusKeyDefinition), cancellationToken: cancellationToken);

            var workflowDefinitionIdAndStatusKeyDefinition = builder.Combine(builder.Ascending(x => x.DefinitionId), builder.Ascending(x => x.WorkflowStatus));
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<WorkflowInstance>(workflowDefinitionIdAndStatusKeyDefinition), cancellationToken: cancellationToken);
        }

        private async Task CreateWorkflowDefinitionsIndexes(ElsaMongoDbContextProvider dbContextProvider, CancellationToken cancellationToken)
        {
            var builder = Builders<WorkflowDefinition>.IndexKeys;
            var tenantKeysDefinition = builder.Ascending(x => x.TenantId);
            var definitionIdKeysDefinition = builder.Ascending(x => x.DefinitionId);
            var versionKeysDefinition = builder.Ascending(x => x.Version);
            var nameKeysDefinition = builder.Ascending(x => x.Name);
            var tagKeysDefinition = builder.Ascending(x => x.Tag);
            var workflowDefinitionIdAndVersionKeyDefinition = builder.Combine(builder.Ascending(x => x.DefinitionId), builder.Ascending(x => x.Version));
            var collection = dbContextProvider.WorkflowDefinitions;
            await CreateIndexesAsync(dbContextProvider.WorkflowDefinitions, cancellationToken, tenantKeysDefinition, definitionIdKeysDefinition, versionKeysDefinition, nameKeysDefinition, tagKeysDefinition);
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<WorkflowDefinition>(workflowDefinitionIdAndVersionKeyDefinition, new CreateIndexOptions { Unique = true }), cancellationToken: cancellationToken);
        }

        private async Task CreateWorkflowExecutionLogIndexes(ElsaMongoDbContextProvider dbContextProvider, CancellationToken cancellationToken)
        {
            var tenantKeysDefinition = Builders<WorkflowExecutionLogRecord>.IndexKeys.Ascending(x => x.TenantId);
            var workflowInstanceIdKeysDefinition = Builders<WorkflowExecutionLogRecord>.IndexKeys.Ascending(x => x.WorkflowInstanceId);
            var timestampKeysDefinition = Builders<WorkflowExecutionLogRecord>.IndexKeys.Ascending(x => x.Timestamp);

            await CreateIndexesAsync(dbContextProvider.WorkflowExecutionLog, cancellationToken, tenantKeysDefinition, workflowInstanceIdKeysDefinition, timestampKeysDefinition);
        }

        private async Task CreateBookmarkIndexes(ElsaMongoDbContextProvider dbContextProvider, CancellationToken cancellationToken)
        {
            var tenantKeysDefinition = Builders<Bookmark>.IndexKeys.Ascending(x => x.TenantId);
            var activityTypeKeysDefinition = Builders<Bookmark>.IndexKeys.Ascending(x => x.ActivityType);
            var hashKeysDefinition = Builders<Bookmark>.IndexKeys.Ascending(x => x.Hash);
            var workflowInstanceIdKeysDefinition = Builders<Bookmark>.IndexKeys.Ascending(x => x.WorkflowInstanceId);
            var correlationIdKeysDefinition = Builders<Bookmark>.IndexKeys.Ascending(x => x.CorrelationId);

            await CreateIndexesAsync(dbContextProvider.Bookmarks, cancellationToken, tenantKeysDefinition, activityTypeKeysDefinition, hashKeysDefinition, workflowInstanceIdKeysDefinition, correlationIdKeysDefinition);
        }

        private async Task CreateIndexesAsync<T>(IMongoCollection<T> collection, CancellationToken cancellationToken, params IndexKeysDefinition<T>[] definitions)
        {
            var models = definitions.Select(x => new CreateIndexModel<T>(x));
            await collection.Indexes.CreateManyAsync(models, cancellationToken);
        }
    }
}