using System.IO.Compression;
using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Authorization;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Api.Permissions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.State;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Import;

/// <summary>Restores validated workflow instance archives without creating executable state.</summary>
[PublicAPI]
internal class Import(
    IWorkflowInstanceManager workflowInstanceManager,
    IWorkflowInstanceStore workflowInstanceStore,
    IWorkflowDefinitionStore definitionStore,
    IActivityExecutionStore activityExecutionStore,
    IWorkflowExecutionLogStore workflowExecutionLogStore,
    IWorkflowStateSerializer workflowStateSerializer,
    ISafeSerializer safeSerializer,
    ITenantAccessor tenantAccessor) : ElsaEndpointWithoutRequest<Response>
{
    private const int MaximumExpandedBytes = 16 * 1024 * 1024;
    private const int MaximumEntries = 256;

    public override void Configure()
    {
        Post("/bulk-actions/import/workflow-instances");
        RequirePermission(WorkflowPermissions.Instances, "restore");
        AllowFileUploads();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true || !HttpContext.GetPermissionEvaluator().HasPermission(User, WorkflowPermissions.Instances, "restore"))
        {
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        List<RestorableArchive> archives;
        try
        {
            archives = await ReadArchivesAsync(Files, cancellationToken);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or FormatException)
        {
            AddError("The archive contains malformed or unsupported workflow data.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        if (archives.Count == 0 || archives.Select(archive => archive.State.Id).Distinct(StringComparer.Ordinal).Count() != archives.Count)
        {
            AddError("Supply at least one archive, with each workflow instance ID appearing only once.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        foreach (var archive in archives)
        {
            if (!await IsRestorableAsync(archive, cancellationToken))
            {
                AddError("Only completed archives for an existing workflow version in the current tenant can be restored.");
                await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
                return;
            }

            if (await workflowInstanceStore.CountAsync(new WorkflowInstanceFilter { Id = archive.State.Id }, cancellationToken) != 0)
            {
                AddError("A workflow instance with this ID already exists.");
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, cancellationToken);
                return;
            }
        }

        foreach (var archive in archives)
        {
            await workflowInstanceManager.CreateAsync(archive.State, cancellationToken);
            var executionIds = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var record in archive.Activities)
            {
                var oldId = record.Id;
                record.Id = Guid.NewGuid().ToString("N");
                executionIds.Add(oldId, record.Id);
                record.TenantId = tenantAccessor.TenantId;
            }

            foreach (var record in archive.Activities)
            {
                if (record.SchedulingActivityExecutionId is { } schedulingId && executionIds.TryGetValue(schedulingId, out var mapped))
                    record.SchedulingActivityExecutionId = mapped;
            }

            foreach (var record in archive.Journal)
            {
                record.Id = Guid.NewGuid().ToString("N");
                record.TenantId = tenantAccessor.TenantId;
                if (executionIds.TryGetValue(record.ActivityInstanceId ?? "", out var activityId))
                    record.ActivityInstanceId = activityId;
                if (record.ParentActivityInstanceId is { } parentId && executionIds.TryGetValue(parentId, out var parentActivityId))
                    record.ParentActivityInstanceId = parentActivityId;
            }

            if (archive.Activities.Count > 0)
                await activityExecutionStore.SaveManyAsync(archive.Activities, cancellationToken);
            if (archive.Journal.Count > 0)
                await workflowExecutionLogStore.SaveManyAsync(archive.Journal, cancellationToken);
        }

        await Send.OkAsync(new Response { Imported = archives.Count }, cancellationToken);
    }

    private async Task<bool> IsRestorableAsync(RestorableArchive archive, CancellationToken cancellationToken)
    {
        var state = archive.State;
        var tenantId = tenantAccessor.TenantId;
        if (!SameTenant(archive.TenantId, tenantId) ||
            string.IsNullOrWhiteSpace(state.Id) ||
            string.IsNullOrWhiteSpace(state.DefinitionId) ||
            string.IsNullOrWhiteSpace(state.DefinitionVersionId) ||
            state.Status != WorkflowStatus.Finished ||
            state.SubStatus is not (WorkflowSubStatus.Finished or WorkflowSubStatus.Cancelled or WorkflowSubStatus.Faulted) ||
            state.IsExecuting || state.Incidents is null || state.Properties is null ||
            state.Bookmarks is not { Count: 0 } ||
            state.ScheduledActivities is not { Count: 0 } || state.CompletionCallbacks is not { Count: 0 } ||
            !archive.NoExternalBookmarks)
            return false;

        var definition = await definitionStore.FindAsync(new WorkflowDefinitionFilter { Id = state.DefinitionVersionId }, cancellationToken);
        if (definition == null ||
            !(SameTenant(definition.TenantId, tenantId) || definition.TenantId == "*") ||
            definition.DefinitionId != state.DefinitionId || definition.Version != state.DefinitionVersion ||
            definition.IsSystem != state.IsSystem)
            return false;

        if (state.ParentWorkflowInstanceId is { } parentId)
        {
            var parent = await workflowInstanceStore.FindAsync(new WorkflowInstanceFilter { Id = parentId }, cancellationToken);
            if (parent is null || parentId == state.Id || !SameTenant(parent.TenantId, tenantId))
                return false;
        }

        return archive.Activities.All(record => record is not null && record.WorkflowInstanceId == state.Id &&
                   !string.IsNullOrWhiteSpace(record.Id) && SameTenant(record.TenantId, tenantId)) &&
               archive.Activities.Select(record => record.Id).Distinct(StringComparer.Ordinal).Count() == archive.Activities.Count &&
               archive.Journal.All(record => record is not null && record.WorkflowInstanceId == state.Id &&
                   record.WorkflowDefinitionId == state.DefinitionId && record.WorkflowDefinitionVersionId == state.DefinitionVersionId &&
                   SameTenant(record.TenantId, tenantId));
    }

    private async Task<List<RestorableArchive>> ReadArchivesAsync(IFormFileCollection files, CancellationToken cancellationToken)
    {
        var result = new List<RestorableArchive>();
        var remaining = MaximumExpandedBytes;
        var buffer = new byte[8192];

        foreach (var file in files)
        {
            await using var stream = file.OpenReadStream();
            if (MediaTypeHeaderValue.TryParse(file.ContentType, out var contentType) &&
                string.Equals(contentType.MediaType.Value, "application/json", StringComparison.OrdinalIgnoreCase))
            {
                await ReadEntryAsync(stream);
                continue;
            }

            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            if (zip.Entries.Count > MaximumEntries)
                throw new InvalidDataException();

            foreach (var entry in zip.Entries.Where(entry => entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
            {
                await using var entryStream = entry.Open();
                await ReadEntryAsync(entryStream);
            }
        }

        return result;

        async Task ReadEntryAsync(Stream stream)
        {
            if (result.Count >= MaximumEntries)
                throw new InvalidDataException();

            using var content = new MemoryStream();
            int count;
            while ((count = await stream.ReadAsync(buffer, cancellationToken)) != 0)
            {
                remaining -= count;
                if (remaining < 0)
                    throw new InvalidDataException();
                await content.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
            }

            content.Position = 0;
            var model = await JsonSerializer.DeserializeAsync<ExportedWorkflowState>(content, cancellationToken: cancellationToken)
                ?? throw new InvalidDataException();
            if (model.WorkflowState.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException();

            var state = workflowStateSerializer.Deserialize(model.WorkflowState) ?? throw new InvalidDataException();
            var noExternalBookmarks = model.Bookmarks is null or { ValueKind: JsonValueKind.Null } ||
                model.Bookmarks is { ValueKind: JsonValueKind.Array } array && array.GetArrayLength() == 0;
            var activities = model.ActivityExecutionRecords is { ValueKind: not JsonValueKind.Null } activitiesJson
                ? safeSerializer.Deserialize<ICollection<ActivityExecutionRecord>>(activitiesJson)
                : [];
            var journal = model.WorkflowExecutionLogRecords is { ValueKind: not JsonValueKind.Null } journalJson
                ? safeSerializer.Deserialize<ICollection<WorkflowExecutionLogRecord>>(journalJson)
                : [];

            result.Add(new(state, model.TenantId, noExternalBookmarks, activities, journal));
        }
    }

    private static bool SameTenant(string? first, string? second) => string.Equals(first ?? "", second ?? "", StringComparison.Ordinal);

    private sealed record RestorableArchive(WorkflowState State, string? TenantId, bool NoExternalBookmarks,
        ICollection<ActivityExecutionRecord> Activities, ICollection<WorkflowExecutionLogRecord> Journal);
}
