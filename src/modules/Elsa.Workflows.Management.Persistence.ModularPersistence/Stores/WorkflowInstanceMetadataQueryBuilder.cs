using Elsa.Common.Models;
using Elsa.ModularPersistence.Queries;
using Elsa.Workflows.Management.Enums;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Persistence.ModularPersistence.Storage;

namespace Elsa.Workflows.Management.Persistence.ModularPersistence.Stores;

internal static class WorkflowInstanceMetadataQueryBuilder
{
    public static DocumentQuery Build(WorkflowInstanceFilter filter, PageArgs? pageArgs = null, string? tenantId = null)
    {
        var filters = BuildFilters(filter).ToArray();
        if (filters.Length == 0)
            filters = [DocumentQueryFilter.IsNotNull(WorkflowInstanceMetadataStorageManifest.IdIndexName, "Id")];
        var page = CreatePage(pageArgs);
        var sorts = new[]
        {
            new DocumentQuerySort(WorkflowInstanceMetadataStorageManifest.CreatedAtIndexName, "CreatedAt")
        };

        return new DocumentQuery(
            WorkflowInstanceMetadataStorageManifest.StorageUnitName,
            filters,
            sorts,
            page,
            tenantId);
    }

    private static IEnumerable<DocumentQueryFilter> BuildFilters(WorkflowInstanceFilter filter)
    {
        RejectScanOnlyFilters(filter);

        if (!string.IsNullOrWhiteSpace(filter.Id))
            yield return DocumentQueryFilter.Equal(WorkflowInstanceMetadataStorageManifest.IdIndexName, "Id", DocumentQueryValue.String(filter.Id));

        if (filter.Ids?.Count > 0)
            yield return DocumentQueryFilter.In(WorkflowInstanceMetadataStorageManifest.IdIndexName, "Id", filter.Ids.Select(DocumentQueryValue.String));

        if (!string.IsNullOrWhiteSpace(filter.DefinitionId))
            yield return DocumentQueryFilter.Equal(WorkflowInstanceMetadataStorageManifest.DefinitionIdIndexName, "DefinitionId", DocumentQueryValue.String(filter.DefinitionId));

        if (!string.IsNullOrWhiteSpace(filter.DefinitionVersionId))
            yield return DocumentQueryFilter.Equal(WorkflowInstanceMetadataStorageManifest.DefinitionVersionIdIndexName, "DefinitionVersionId", DocumentQueryValue.String(filter.DefinitionVersionId));

        if (filter.DefinitionIds?.Count > 0)
            yield return DocumentQueryFilter.In(WorkflowInstanceMetadataStorageManifest.DefinitionIdIndexName, "DefinitionId", filter.DefinitionIds.Select(DocumentQueryValue.String));

        if (filter.DefinitionVersionIds?.Count > 0)
            yield return DocumentQueryFilter.In(WorkflowInstanceMetadataStorageManifest.DefinitionVersionIdIndexName, "DefinitionVersionId", filter.DefinitionVersionIds.Select(DocumentQueryValue.String));

        if (filter.Version != null)
            yield return DocumentQueryFilter.Equal(WorkflowInstanceMetadataStorageManifest.VersionIndexName, "Version", DocumentQueryValue.Int32(filter.Version.Value));

        if (filter.ParentWorkflowInstanceIds?.Count > 0)
            yield return DocumentQueryFilter.In(WorkflowInstanceMetadataStorageManifest.ParentWorkflowInstanceIdIndexName, "ParentWorkflowInstanceId", filter.ParentWorkflowInstanceIds.Select(DocumentQueryValue.String));

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
            yield return DocumentQueryFilter.Equal(WorkflowInstanceMetadataStorageManifest.CorrelationIdIndexName, "CorrelationId", DocumentQueryValue.String(filter.CorrelationId));

        if (filter.CorrelationIds?.Count > 0)
            yield return DocumentQueryFilter.In(WorkflowInstanceMetadataStorageManifest.CorrelationIdIndexName, "CorrelationId", filter.CorrelationIds.Select(DocumentQueryValue.String));

        if (filter.Names?.Count > 0)
            yield return DocumentQueryFilter.In(WorkflowInstanceMetadataStorageManifest.NameIndexName, "Name", filter.Names.Select(DocumentQueryValue.String));

        if (filter.WorkflowStatus != null)
            yield return DocumentQueryFilter.Equal(WorkflowInstanceMetadataStorageManifest.StatusIndexName, "Status", DocumentQueryValue.String(filter.WorkflowStatus.Value.ToString()));

        if (filter.WorkflowStatuses?.Count > 0)
            yield return DocumentQueryFilter.In(WorkflowInstanceMetadataStorageManifest.StatusIndexName, "Status", filter.WorkflowStatuses.Select(x => DocumentQueryValue.String(x.ToString())));

        if (filter.WorkflowSubStatus != null)
            yield return DocumentQueryFilter.Equal(WorkflowInstanceMetadataStorageManifest.SubStatusIndexName, "SubStatus", DocumentQueryValue.String(filter.WorkflowSubStatus.Value.ToString()));

        if (filter.WorkflowSubStatuses?.Count > 0)
            yield return DocumentQueryFilter.In(WorkflowInstanceMetadataStorageManifest.SubStatusIndexName, "SubStatus", filter.WorkflowSubStatuses.Select(x => DocumentQueryValue.String(x.ToString())));

        if (filter.IsExecuting != null)
            yield return DocumentQueryFilter.Equal(WorkflowInstanceMetadataStorageManifest.IsExecutingIndexName, "IsExecuting", DocumentQueryValue.Boolean(filter.IsExecuting.Value));

        if (filter.HasIncidents != null)
        {
            yield return filter.HasIncidents.Value
                ? DocumentQueryFilter.GreaterThan(WorkflowInstanceMetadataStorageManifest.HasIncidentsIndexName, "IncidentCount", DocumentQueryValue.Int32(0))
                : DocumentQueryFilter.Equal(WorkflowInstanceMetadataStorageManifest.HasIncidentsIndexName, "IncidentCount", DocumentQueryValue.Int32(0));
        }

        if (filter.IsSystem != null)
            yield return DocumentQueryFilter.Equal(WorkflowInstanceMetadataStorageManifest.IsSystemIndexName, "IsSystem", DocumentQueryValue.Boolean(filter.IsSystem.Value));

        if (filter.BeforeLastUpdated != null)
            yield return DocumentQueryFilter.LessThan(WorkflowInstanceMetadataStorageManifest.UpdatedAtIndexName, "UpdatedAt", DocumentQueryValue.DateTimeOffset(filter.BeforeLastUpdated.Value));

        foreach (var timestampFilter in BuildTimestampFilters(filter.TimestampFilters))
            yield return timestampFilter;
    }

    private static void RejectScanOnlyFilters(WorkflowInstanceFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            throw new WorkflowInstanceMetadataQueryException("Workflow instance metadata persistence does not support SearchTerm because it requires provider-specific full-text or contains scanning.");

        if (!string.IsNullOrWhiteSpace(filter.Name))
            throw new WorkflowInstanceMetadataQueryException("Workflow instance metadata persistence supports exact Names matches only. The Name contains filter is intentionally not supported by the metadata projection.");
    }

    private static IEnumerable<DocumentQueryFilter> BuildTimestampFilters(IEnumerable<TimestampFilter>? timestampFilters)
    {
        foreach (var error in WorkflowInstanceFilter.ValidateTimestampFilters(timestampFilters))
            throw new WorkflowInstanceMetadataQueryException(error);

        if (timestampFilters == null)
            yield break;

        foreach (var timestampFilter in timestampFilters)
        {
            if (!WorkflowInstanceFilter.TryNormalizeTimestampFilterColumn(timestampFilter.Column, out var column, out var error))
                throw new WorkflowInstanceMetadataQueryException(error);

            var indexName = GetTimestampIndexName(column);
            var timestamp = timestampFilter.Timestamp;
            var isZeroTime = timestamp.TimeOfDay == TimeSpan.Zero;
            var startDay = new DateTimeOffset(timestamp.Date);
            var endDay = startDay.AddDays(1);

            switch (timestampFilter.Operator)
            {
                case TimestampFilterOperator.Is when isZeroTime:
                    yield return DocumentQueryFilter.GreaterThanOrEqual(indexName, column, DocumentQueryValue.DateTimeOffset(startDay));
                    yield return DocumentQueryFilter.LessThan(indexName, column, DocumentQueryValue.DateTimeOffset(endDay));
                    break;
                case TimestampFilterOperator.Is:
                    yield return DocumentQueryFilter.Equal(indexName, column, DocumentQueryValue.DateTimeOffset(timestamp));
                    break;
                case TimestampFilterOperator.IsNot:
                    throw new WorkflowInstanceMetadataQueryException("Workflow instance metadata persistence does not support TimestampFilterOperator.IsNot because it cannot be represented as a single portable index range.");
                case TimestampFilterOperator.GreaterThan when isZeroTime:
                    yield return DocumentQueryFilter.GreaterThan(indexName, column, DocumentQueryValue.DateTimeOffset(endDay));
                    break;
                case TimestampFilterOperator.GreaterThan:
                    yield return DocumentQueryFilter.GreaterThan(indexName, column, DocumentQueryValue.DateTimeOffset(timestamp));
                    break;
                case TimestampFilterOperator.GreaterThanOrEqual when isZeroTime:
                    yield return DocumentQueryFilter.GreaterThanOrEqual(indexName, column, DocumentQueryValue.DateTimeOffset(startDay));
                    break;
                case TimestampFilterOperator.GreaterThanOrEqual:
                    yield return DocumentQueryFilter.GreaterThanOrEqual(indexName, column, DocumentQueryValue.DateTimeOffset(timestamp));
                    break;
                case TimestampFilterOperator.LessThan when isZeroTime:
                    yield return DocumentQueryFilter.LessThan(indexName, column, DocumentQueryValue.DateTimeOffset(startDay));
                    break;
                case TimestampFilterOperator.LessThan:
                    yield return DocumentQueryFilter.LessThan(indexName, column, DocumentQueryValue.DateTimeOffset(timestamp));
                    break;
                case TimestampFilterOperator.LessThanOrEqual when isZeroTime:
                    yield return DocumentQueryFilter.LessThanOrEqual(indexName, column, DocumentQueryValue.DateTimeOffset(endDay));
                    break;
                case TimestampFilterOperator.LessThanOrEqual:
                    yield return DocumentQueryFilter.LessThanOrEqual(indexName, column, DocumentQueryValue.DateTimeOffset(timestamp));
                    break;
            }
        }
    }

    private static string GetTimestampIndexName(string column) =>
        column switch
        {
            "CreatedAt" => WorkflowInstanceMetadataStorageManifest.CreatedAtIndexName,
            "UpdatedAt" => WorkflowInstanceMetadataStorageManifest.UpdatedAtIndexName,
            "FinishedAt" => WorkflowInstanceMetadataStorageManifest.FinishedAtIndexName,
            _ => throw new WorkflowInstanceMetadataQueryException($"Unsupported timestamp column '{column}'.")
        };

    private static DocumentQueryPage? CreatePage(PageArgs? pageArgs)
    {
        if (pageArgs?.Limit is null)
            return null;

        return new DocumentQueryPage(pageArgs.Limit.Value, pageArgs.Offset ?? 0);
    }
}
