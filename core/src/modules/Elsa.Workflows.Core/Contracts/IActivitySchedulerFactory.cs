namespace Elsa.Workflows;

public interface IActivitySchedulerFactory
{
    IActivityScheduler CreateScheduler();
}