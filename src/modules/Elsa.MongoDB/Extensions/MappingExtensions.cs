using Elsa.Common.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Models;
using WorkflowDefinition = Elsa.Workflows.Management.Entities.WorkflowDefinition;
using WorkflowInstance = Elsa.Workflows.Management.Entities.WorkflowInstance;
using WorkflowState = Elsa.Workflows.Core.State.WorkflowState;
using WorkflowExecutionLogRecord = Elsa.Workflows.Runtime.Entities.WorkflowExecutionLogRecord;
using StoredBookmark = Elsa.Workflows.Runtime.Entities.StoredBookmark;

namespace Elsa.MongoDB.Extensions;

public static class MappingExtensions
{
    /// <summary>
    /// Maps a workflow definition to a document.
    /// </summary>
    public static Models.WorkflowDefinition MapToDocument(this WorkflowDefinition definition, IPayloadSerializer serializer)
    {
        var data = new WorkflowDefinitionState(definition.Options, definition.Variables, definition.Inputs, definition.Outputs, definition.Outcomes, definition.CustomProperties);
        var json = serializer.Serialize(data);

        return new Models.WorkflowDefinition
        {
            DefinitionId = definition.DefinitionId,
            Version = definition.Version,
            Name = definition.Name,
            Description = definition.Description,
            BinaryData = definition.BinaryData,
            StringData = definition.StringData,
            MaterializerName = definition.MaterializerName,
            MaterializerContext = definition.MaterializerContext,
            UsableAsActivity = definition.UsableAsActivity,
            CreatedAt = definition.CreatedAt,
            IsLatest = definition.IsLatest,
            IsPublished = definition.IsPublished,
            Data = json
        };
    }
    
    /// <summary>
    /// Maps a document to a workflow definition.
    /// </summary>
    public static WorkflowDefinition MapFromDocument(this Models.WorkflowDefinition definition, IPayloadSerializer serializer)
    {
        var data = string.IsNullOrWhiteSpace(definition.Data) ? default : serializer.Deserialize<WorkflowDefinitionState>(definition.Data);

        return new WorkflowDefinition
        {
            DefinitionId = definition.DefinitionId,
            Version = definition.Version,
            Name = definition.Name,
            Description = definition.Description,
            BinaryData = definition.BinaryData,
            StringData = definition.StringData,
            MaterializerName = definition.MaterializerName,
            MaterializerContext = definition.MaterializerContext,
            UsableAsActivity = definition.UsableAsActivity,
            CreatedAt = definition.CreatedAt,
            IsLatest = definition.IsLatest,
            IsPublished = definition.IsPublished,
            Options = data?.Options,
            Variables = data?.Variables!,
            Inputs = data?.Inputs!,
            Outputs = data?.Outputs!,
            Outcomes = data?.Outcomes!,
            CustomProperties = data?.CustomProperties!
        };
    }
    
    /// <summary>
    /// Maps a workflow instance to a document.
    /// </summary>
    public static Models.WorkflowInstance MapToDocument(this WorkflowInstance instance, IWorkflowStateSerializer serializer)
    {
        var json = serializer.Serialize(instance.WorkflowState);

        return new Models.WorkflowInstance
        {
            DefinitionId = instance.DefinitionId,
            DefinitionVersionId = instance.DefinitionVersionId,
            Version = instance.Version,
            Status = instance.Status,
            SubStatus = instance.SubStatus,
            CorrelationId = instance.CorrelationId,
            CreatedAt = instance.CreatedAt,
            LastExecutedAt = instance.LastExecutedAt,
            FinishedAt = instance.FinishedAt,
            FaultedAt = instance.FaultedAt,
            CancelledAt = instance.CancelledAt,
            Name = instance.Name,
            Data = json
        };
    }
    
    /// <summary>
    /// Maps a document to a workflow instance.
    /// </summary>
    public static WorkflowInstance MapFromDocument(this Models.WorkflowInstance instance, IWorkflowStateSerializer serializer)
    {
        var state = string.IsNullOrWhiteSpace(instance.Data) ? default : serializer.Deserialize(instance.Data);

        return new WorkflowInstance
        {
            DefinitionId = instance.DefinitionId,
            DefinitionVersionId = instance.DefinitionVersionId,
            Version = instance.Version,
            Status = instance.Status,
            SubStatus = instance.SubStatus,
            CorrelationId = instance.CorrelationId,
            CreatedAt = instance.CreatedAt,
            LastExecutedAt = instance.LastExecutedAt,
            FinishedAt = instance.FinishedAt,
            FaultedAt = instance.FaultedAt,
            CancelledAt = instance.CancelledAt,
            Name = instance.Name,
            WorkflowState = state!
        };
    }
    
    /// <summary>
    /// Maps a workflow state to a document.
    /// </summary>
    public static Models.WorkflowState MapToDocument(this WorkflowState state, IWorkflowStateSerializer serializer)
    {
        var json = serializer.Serialize(state);
        
        return new Models.WorkflowState
        {
            Id = state.Id,
            DefinitionId = state.DefinitionId,
            DefinitionVersion = state.DefinitionVersion,
            Status = state.Status,
            SubStatus = state.SubStatus,
            Data = json
        };
    }
    
    /// <summary>
    /// Maps a document to a workflow state.
    /// </summary>
    public static WorkflowState MapFromDocument(this Models.WorkflowState state, IWorkflowStateSerializer serializer)
    {
        return serializer.Deserialize(state.Data);
    }
    
    /// <summary>
    /// Maps a workflow execution log record to a document.
    /// </summary>
    public static Models.WorkflowExecutionLogRecord MapToDocument(this WorkflowExecutionLogRecord logRecord, IPayloadSerializer serializer)
    {
        var payloadData = logRecord.Payload != null ? serializer.Serialize(logRecord.Payload) : default;
        var activityData = logRecord.ActivityState != null ? serializer.Serialize(logRecord.ActivityState) : default;

        return new Models.WorkflowExecutionLogRecord
        {
            Id = logRecord.Id,
            WorkflowDefinitionId = logRecord.WorkflowDefinitionId,
            WorkflowInstanceId = logRecord.WorkflowInstanceId,
            WorkflowVersion = logRecord.WorkflowVersion,
            ActivityInstanceId = logRecord.ActivityInstanceId,
            ParentActivityInstanceId = logRecord.ParentActivityInstanceId,
            ActivityId = logRecord.ActivityId,
            ActivityType = logRecord.ActivityType,
            NodeId = logRecord.NodeId,
            Timestamp = logRecord.Timestamp,
            EventName = logRecord.EventName,
            Source = logRecord.Source,
            Message = logRecord.Message,
            PayloadData = payloadData,
            ActivityData = activityData
        };
    }

    /// <summary>
    /// Maps a document to a workflow execution log record.
    /// </summary>
    public static WorkflowExecutionLogRecord MapFromDocument(this Models.WorkflowExecutionLogRecord logRecord, IPayloadSerializer serializer)
    {
        var payload = !string.IsNullOrEmpty(logRecord.PayloadData) ? serializer.Deserialize(logRecord.PayloadData) : null;
        var activityState = !string.IsNullOrEmpty(logRecord.ActivityData) ? serializer.Deserialize<IDictionary<string, object>>(logRecord.ActivityData) : null;
        
        return new WorkflowExecutionLogRecord
        {
            Id = logRecord.Id,
            WorkflowDefinitionId = logRecord.WorkflowDefinitionId,
            WorkflowInstanceId = logRecord.WorkflowInstanceId,
            WorkflowVersion = logRecord.WorkflowVersion,
            ActivityInstanceId = logRecord.ActivityInstanceId,
            ParentActivityInstanceId = logRecord.ParentActivityInstanceId,
            ActivityId = logRecord.ActivityId,
            ActivityType = logRecord.ActivityType,
            NodeId = logRecord.NodeId,
            Timestamp = logRecord.Timestamp,
            EventName = logRecord.EventName,
            Source = logRecord.Source,
            Message = logRecord.Message,
            Payload = payload,
            ActivityState = activityState
        };
    }

    /// <summary>
    /// Maps a bookmark to a document.
    /// </summary>
    public static Models.StoredBookmark MapToDocument(this StoredBookmark bookmark, IPayloadSerializer serializer)
    {
        var payloadData = bookmark.Payload != null ? serializer.Serialize(bookmark.Payload) : default;
        
        return new Models.StoredBookmark
        {
            Id = bookmark.BookmarkId,
            BookmarkId = bookmark.BookmarkId,
            ActivityTypeName = bookmark.ActivityTypeName,
            Hash = bookmark.Hash,
            WorkflowInstanceId = bookmark.WorkflowInstanceId,
            CorrelationId = bookmark.CorrelationId,
            Data = payloadData
        };
    }
    
    /// <summary>
    /// Maps a document to a bookmark.
    /// </summary>
    public static StoredBookmark MapFromDocument(this Models.StoredBookmark bookmark, IPayloadSerializer serializer)
    {
        var payload = !string.IsNullOrEmpty(bookmark.Data) ? serializer.Deserialize(bookmark.Data) : null;
        
        return new StoredBookmark
        {
            BookmarkId = bookmark.BookmarkId,
            ActivityTypeName = bookmark.ActivityTypeName,
            Hash = bookmark.Hash,
            WorkflowInstanceId = bookmark.WorkflowInstanceId,
            CorrelationId = bookmark.CorrelationId,
            Payload = payload
        };
    }
}