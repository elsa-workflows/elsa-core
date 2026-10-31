using Quartz;

namespace Elsa.Scheduling.Quartz.Contracts;

public interface IJobKeyProvider
{
    JobKey GetJobKey<TJob>() where TJob : IJob;
    string GetGroupName();
}