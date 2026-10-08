using System.Text.Json;
using Elsa.Workflows.Api.Endpoints.BookmarkQueueDeadLetters;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.ComponentTests.Scenarios.RestApis.Endpoints.BookmarkQueueDeadLetters;

public class BookmarkQueueDeadLetterModelTests
{
    [Fact]
    public void FromEntity_MapsSafeFieldsAndOmitsResumeOptions()
    {
        var originalCreatedAt = new DateTimeOffset(2026, 5, 20, 10, 0, 0, TimeSpan.Zero);
        var deadLetteredAt = originalCreatedAt.AddMinutes(1);
        var lastAttemptedAt = originalCreatedAt.AddSeconds(30);
        var replayedAt = originalCreatedAt.AddMinutes(2);
        var item = new BookmarkQueueDeadLetterItem
        {
            Id = "dead-letter",
            TenantId = "tenant",
            OriginalQueueItemId = "queue-item",
            WorkflowInstanceId = "workflow-instance",
            CorrelationId = "correlation",
            BookmarkId = "bookmark",
            StimulusHash = "stimulus-hash",
            ActivityInstanceId = "activity-instance",
            ActivityTypeName = "activity",
            Options = new ResumeBookmarkOptions
            {
                Input = new Dictionary<string, object> { ["secret"] = "input-secret" },
                Properties = new Dictionary<string, object> { ["secret"] = "property-secret" }
            },
            OriginalCreatedAt = originalCreatedAt,
            DeadLetteredAt = deadLetteredAt,
            Reason = "Failed",
            DeliveryAttempts = 3,
            LastAttemptedAt = lastAttemptedAt,
            LastErrorType = typeof(InvalidOperationException).FullName,
            LastErrorMessage = "resume failed",
            CanReplay = false,
            ReplayedAt = replayedAt,
            ReplayedQueueItemId = "replayed-queue-item"
        };

        var model = BookmarkQueueDeadLetterModel.FromEntity(item);

        Assert.Equal(item.Id, model.Id);
        Assert.Equal(item.TenantId, model.TenantId);
        Assert.Equal(item.OriginalQueueItemId, model.OriginalQueueItemId);
        Assert.Equal(item.WorkflowInstanceId, model.WorkflowInstanceId);
        Assert.Equal(item.CorrelationId, model.CorrelationId);
        Assert.Equal(item.BookmarkId, model.BookmarkId);
        Assert.Equal(item.StimulusHash, model.StimulusHash);
        Assert.Equal(item.ActivityInstanceId, model.ActivityInstanceId);
        Assert.Equal(item.ActivityTypeName, model.ActivityTypeName);
        Assert.Equal(item.OriginalCreatedAt, model.OriginalCreatedAt);
        Assert.Equal(item.DeadLetteredAt, model.DeadLetteredAt);
        Assert.Equal(item.Reason, model.Reason);
        Assert.Equal(item.DeliveryAttempts, model.DeliveryAttempts);
        Assert.Equal(item.LastAttemptedAt, model.LastAttemptedAt);
        Assert.Equal(item.LastErrorType, model.LastErrorType);
        Assert.Equal(item.LastErrorMessage, model.LastErrorMessage);
        Assert.Equal(item.CanReplay, model.CanReplay);
        Assert.Equal(item.ReplayedAt, model.ReplayedAt);
        Assert.Equal(item.ReplayedQueueItemId, model.ReplayedQueueItemId);
        Assert.Null(typeof(BookmarkQueueDeadLetterModel).GetProperty(nameof(BookmarkQueueDeadLetterItem.Options)));

        var json = JsonSerializer.Serialize(model);
        Assert.DoesNotContain("input-secret", json);
        Assert.DoesNotContain("property-secret", json);
    }
}
