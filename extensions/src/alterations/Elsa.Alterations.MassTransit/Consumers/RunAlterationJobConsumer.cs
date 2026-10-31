using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.MassTransit.Messages;
using MassTransit;

namespace Elsa.Alterations.MassTransit.Consumers;

/// <summary>
/// Consumes <see cref="RunAlterationJob"/> messages.
/// </summary>
public class RunAlterationJobConsumer : IConsumer<RunAlterationJob>
{
    private readonly IAlterationJobRunner _alterationJobRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunAlterationJobConsumer"/> class.
    /// </summary>
    public RunAlterationJobConsumer(IAlterationJobRunner alterationJobRunner)
    {
        _alterationJobRunner = alterationJobRunner;
    }
    
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<RunAlterationJob> context)
    {
        await _alterationJobRunner.RunAsync(context.Message.JobId, context.CancellationToken);
    }
}