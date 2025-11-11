using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Elsa.Persistence.EFCore.MySql.Services;

/// <summary>
/// Provides an implementation of <see cref="IWorkflowReferenceQuery"/> for querying MySQL databases
/// to find all latest workflow definitions that reference a specific workflow definition.
/// </summary>
public class MySqlWorkflowReferenceQuery(IDbContextFactory<ManagementElsaDbContext> dbContextFactory) : IWorkflowReferenceQuery
{
    public async Task<IEnumerable<string>> ExecuteAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tableName = dbContext.Model.FindEntityType(typeof(WorkflowDefinition))!.GetTableName();

        var sql = $"""
                     SELECT
                     t.DefinitionId,
                     t.TenantId
                   FROM {tableName} AS t
                   WHERE
                     t.IsLatest = 1
                     AND EXISTS (
                       SELECT 1
                       FROM {tableName} AS t2
                       CROSS JOIN JSON_TABLE(
                         t2.StringData,
                         '$.activities[*]'
                         COLUMNS (
                           workflowDefinitionId            VARCHAR(255) PATH '$.workflowDefinitionId',
                           latestAvailablePublishedVersion INT           PATH '$.latestAvailablePublishedVersion'
                         )
                       ) AS act
                       WHERE
                         t2.DefinitionId = t.DefinitionId
                         AND act.workflowDefinitionId = @p0
                         AND act.latestAvailablePublishedVersion IS NOT NULL
                     )
                   """;

        var param = new MySqlParameter("@p0", workflowDefinitionId);

        return await dbContext.Set<WorkflowDefinition>()
            .FromSqlRaw(sql, param)
            .Select(x => x.DefinitionId)
            .ToListAsync(cancellationToken);
    }
}