using Quartz;

namespace Elsa.Quartz.Contracts;

internal interface IJobKeyProvider
{
    JobKey GetJobKey<TJob>() where TJob : IJob;
    string GetGroupName();
}