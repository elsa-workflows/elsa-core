using Elsa.Resilience.Entities;
using Elsa.Resilience.Models;

namespace Elsa.Resilience.Core.UnitTests;

public class VoidRetryAttemptServiceTests
{
    [Fact(DisplayName = "Void reader should return an empty page")]
    public async Task ReadAttemptsAsync_ReturnsEmptyPage()
    {
        var page = await VoidRetryAttemptReader.Instance.ReadAttemptsAsync("activity-instance-1");

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact(DisplayName = "Void reader instance should be a singleton")]
    public void ReaderInstance_IsSingleton()
    {
        Assert.Same(VoidRetryAttemptReader.Instance, VoidRetryAttemptReader.Instance);
    }

    [Fact(DisplayName = "Void recorder should discard records without faulting")]
    public async Task RecordAsync_DiscardsRecords()
    {
        var context = new RecordRetryAttemptsContext(null!, [new RetryAttemptRecord()], CancellationToken.None);

        await VoidRetryAttemptRecorder.Instance.RecordAsync(context);
    }

    [Fact(DisplayName = "Void recorder instance should be a singleton")]
    public void RecorderInstance_IsSingleton()
    {
        Assert.Same(VoidRetryAttemptRecorder.Instance, VoidRetryAttemptRecorder.Instance);
    }
}
