namespace Elsa.Services;

public interface IActivitySchedulerFactory
{
    IActivityScheduler CreateScheduler();
}