using System.Data.SqlClient;
using System.Text;
using Dapper;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Dapper.Contracts;
using Elsa.Dapper.Extensions;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Dapper.Modules.Management;

/// <summary>
/// Provides a Dapper implementation of <see cref="IWorkflowDefinitionStore"/>.
/// </summary>
public class DapperWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly IDbConnectionProvider _dbConnectionProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperWorkflowDefinitionStore"/> class.
    /// </summary>
    public DapperWorkflowDefinitionStore(IDbConnectionProvider dbConnectionProvider)
    {
        _dbConnectionProvider = dbConnectionProvider;
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var sql = CreateSql(filter);
    }

    private string CreateSql(WorkflowDefinitionFilter filter)
    {
        var parameters = new DynamicParameters();

        var sql = new StringBuilder()
            .From("WorkflowDefinitions")
            .And(nameof(WorkflowDefinition.DefinitionId), parameters, filter.DefinitionId)
            .And(nameof(WorkflowDefinition.DefinitionId), parameters, filter.DefinitionIds)
            .And(nameof(WorkflowDefinition.Id), parameters, filter.Id)
            .And(nameof(WorkflowDefinition.Id), parameters, filter.Ids)
            .And(parameters, filter.VersionOptions)
            .And(nameof(WorkflowDefinition.MaterializerName), parameters, filter.MaterializerName)
            .And(nameof(WorkflowDefinition.Name), parameters, filter.Name)
            .And(nameof(WorkflowDefinition.Name), parameters, filter.Names)
            .And(nameof(WorkflowDefinition.Options.UsableAsActivity), parameters, filter.UsableAsActivity);

        return sql.ToString();
    }


    public Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}