using Elsa.Workflows.Core.Models;

namespace Elsa.Scheduling.Services;

public interface IElasticCongurationScheduler
{
    Task ScheduleAsync(CancellationToken cancellationToken = default);
}