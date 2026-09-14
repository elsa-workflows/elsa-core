using System.Text.Json;
using Elsa.Workflows.Api.Endpoints.BookmarkQueueDeadLetters;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.ComponentTests.Scenarios.RestApis.Endpoints.BookmarkQueueDeadLetters;

public class BookmarkQueueDeadLetterModelTests
{
    [Test]
    public async Task FromEntity_MapsSafeFieldsAndOmitsResumeOptions()
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

        await Assert.That(model.Id).IsEqualTo(item.Id);
        await Assert.That(model.TenantId).IsEqualTo(item.TenantId);
        await Assert.That(model.OriginalQueueItemId).IsEqualTo(item.OriginalQueueItemId);
        await Assert.That(model.WorkflowInstanceId).IsEqualTo(item.WorkflowInstanceId);
        await Assert.That(model.CorrelationId).IsEqualTo(item.CorrelationId);
        await Assert.That(model.BookmarkId).IsEqualTo(item.BookmarkId);
        await Assert.That(model.StimulusHash).IsEqualTo(item.StimulusHash);
        await Assert.That(model.ActivityInstanceId).IsEqualTo(item.ActivityInstanceId);
        await Assert.That(model.ActivityTypeName).IsEqualTo(item.ActivityTypeName);
        await Assert.That(model.OriginalCreatedAt).IsEqualTo(item.OriginalCreatedAt);
        await Assert.That(model.DeadLetteredAt).IsEqualTo(item.DeadLetteredAt);
        await Assert.That(model.Reason).IsEqualTo(item.Reason);
        await Assert.That(model.DeliveryAttempts).IsEqualTo(item.DeliveryAttempts);
        await Assert.That(model.LastAttemptedAt).IsEqualTo(item.LastAttemptedAt);
        await Assert.That(model.LastErrorType).IsEqualTo(item.LastErrorType);
        await Assert.That(model.LastErrorMessage).IsEqualTo(item.LastErrorMessage);
        await Assert.That(model.CanReplay).IsEqualTo(item.CanReplay);
        await Assert.That(model.ReplayedAt).IsEqualTo(item.ReplayedAt);
        await Assert.That(model.ReplayedQueueItemId).IsEqualTo(item.ReplayedQueueItemId);
        await Assert.That(typeof(BookmarkQueueDeadLetterModel).GetProperty(nameof(BookmarkQueueDeadLetterItem.Options))).IsNull();

        var json = JsonSerializer.Serialize(model);
        await Assert.That(json).DoesNotContain("input-secret");
        await Assert.That(json).DoesNotContain("property-secret");
    }
}