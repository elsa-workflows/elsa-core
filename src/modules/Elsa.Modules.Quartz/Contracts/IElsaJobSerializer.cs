using Elsa.Activities.Scheduling.Contracts;
using IElsaJob = Elsa.Activities.Scheduling.Contracts.IJob;

namespace Elsa.Modules.Quartz.Contracts;

public interface IElsaJobSerializer
{
    string Serialize(IJob job);
    T Deserialize<T>(string json) where T : IElsaJob;
}