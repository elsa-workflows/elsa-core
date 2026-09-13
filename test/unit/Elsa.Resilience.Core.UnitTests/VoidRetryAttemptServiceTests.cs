using Elsa.Resilience.Entities;
using Elsa.Resilience.Models;
using System.Threading.Tasks;

namespace Elsa.Resilience.Core.UnitTests;

public class VoidRetryAttemptServiceTests
{
    [Test]
    [DisplayName("Void reader should return an empty page")]
    public async Task ReadAttemptsAsync_ReturnsEmptyPage()
    {
        var page = await VoidRetryAttemptReader.Instance.ReadAttemptsAsync("activity-instance-1");

        await Assert.That(page.Items).IsEmpty();
        await Assert.That(page.TotalCount).IsEqualTo(0);
    }

    [Test]
    [DisplayName("Void reader instance should be a singleton")]
    public async Task ReaderInstance_IsSingleton()
    {
        await Assert.That(VoidRetryAttemptReader.Instance).IsSameReferenceAs(VoidRetryAttemptReader.Instance);
    }

    [Test]
    [DisplayName("Void recorder should discard records without faulting")]
    public async Task RecordAsync_DiscardsRecords()
    {
        var context = new RecordRetryAttemptsContext(null!, [new RetryAttemptRecord()], CancellationToken.None);

        await VoidRetryAttemptRecorder.Instance.RecordAsync(context);
    }

    [Test]
    [DisplayName("Void recorder instance should be a singleton")]
    public async Task RecorderInstance_IsSingleton()
    {
        await Assert.That(VoidRetryAttemptRecorder.Instance).IsSameReferenceAs(VoidRetryAttemptRecorder.Instance);
    }
}
