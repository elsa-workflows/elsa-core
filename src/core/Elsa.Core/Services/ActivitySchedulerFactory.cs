using Elsa.Contracts;

namespace Elsa.Services;

public class ActivitySchedulerFactory : IActivitySchedulerFactory
{
    public IActivityScheduler CreateScheduler() => new ActivityScheduler();
}