using Elsa.Services;

namespace Elsa.Implementations;

public class ActivitySchedulerFactory : IActivitySchedulerFactory
{
    public IActivityScheduler CreateScheduler() => new ActivityScheduler();
}