using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Sqlite.Services;

/// <summary>
/// Provides an implementation of <see cref="IWorkflowReferenceQuery"/> for querying SQLite databases
/// to find all latest workflow definitions that reference a specific workflow definition.
/// </summary>
public class SqliteWorkflowReferenceQuery(IDbContextFactory<ManagementElsaDbContext> dbContextFactory) : IWorkflowReferenceQuery
{
    public async Task<IEnumerable<string>> ExecuteAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tableName = dbContext.Model.FindEntityType(typeof(WorkflowDefinition))!.GetTableName();
        
        var sql = $"""
                    SELECT DefinitionId, TenantId FROM {tableName} WHERE IsLatest = 1 AND EXISTS (
                    SELECT 1 FROM json_each(json_extract(StringData, '$.activities'))
                    WHERE json_extract(value, '$.workflowDefinitionId') = @p0
                    AND json_extract(value, '$.latestAvailablePublishedVersion') IS NOT NULL)
                   """;
        
        return await dbContext.Set<WorkflowDefinition>()
            .FromSqlRaw(sql, workflowDefinitionId)
            .Select(x => x.DefinitionId)
            .ToListAsync(cancellationToken);
    }
}