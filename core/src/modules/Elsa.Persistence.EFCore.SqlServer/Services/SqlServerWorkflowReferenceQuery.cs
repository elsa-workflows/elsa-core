using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.SqlServer.Services;

/// <summary>
/// Provides an implementation of <see cref="IWorkflowReferenceQuery"/> for querying SQL Server databases
/// to find all latest workflow definitions that reference a specific workflow definition.
/// </summary>
public class SqlServerWorkflowReferenceQuery(IDbContextFactory<ManagementElsaDbContext> dbContextFactory) : IWorkflowReferenceQuery
{
    public async Task<IEnumerable<string>> ExecuteAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tableName = dbContext.Model.FindEntityType(typeof(WorkflowDefinition))!.GetSchemaQualifiedTableName();
        
        var sql = $"""
                  SELECT [DefinitionId], [TenantId] FROM {tableName} WHERE [IsLatest] = 1 AND EXISTS (
                  SELECT 1 FROM OPENJSON(JSON_QUERY([StringData], '$.activities')) AS value
                  WHERE JSON_VALUE(value, '$.workflowDefinitionId') = @p0
                  AND JSON_VALUE(value, '$.latestAvailablePublishedVersion') IS NOT NULL)
                  """;
        
        return await dbContext.Set<WorkflowDefinition>()
            .FromSqlRaw(sql, workflowDefinitionId)
            .Select(x => x.DefinitionId)
            .ToListAsync(cancellationToken);
    }
}
