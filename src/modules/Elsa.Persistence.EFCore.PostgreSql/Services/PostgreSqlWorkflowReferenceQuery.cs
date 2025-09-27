using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.PostgreSql.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.PostgreSql.Services;

/// <summary>
/// Provides an implementation of <see cref="IWorkflowReferenceQuery"/> for querying PostgreSQL databases
/// to find all latest workflow definitions that reference a specific workflow definition.
/// </summary>
public class PostgreSqlWorkflowReferenceQuery(IDbContextFactory<ManagementElsaDbContext> dbContextFactory) : IWorkflowReferenceQuery
{
    public async Task<IEnumerable<string>> ExecuteAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tableName = dbContext.Model.FindEntityType(typeof(WorkflowDefinition))!.QuoteSchemaQualifiedTableName();

        var sql = $"""
                   SELECT "DefinitionId", "TenantId" FROM {tableName} WHERE "IsLatest" = true AND EXISTS (
                   SELECT 1 FROM jsonb_array_elements("StringData"::jsonb->'activities') AS value
                   WHERE value->>'workflowDefinitionId' = @p0
                     AND value->'latestAvailablePublishedVersion' IS NOT NULL)
                   """;

        return await dbContext.Set<WorkflowDefinition>()
            .FromSqlRaw(sql, workflowDefinitionId)
            .Select(x => x.DefinitionId)
            .ToListAsync(cancellationToken);
    }
}