namespace Elsa.Contracts;

public interface IActivitySchedulerFactory
{
    IActivityScheduler CreateScheduler();
}