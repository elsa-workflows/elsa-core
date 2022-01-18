using IElsaJob = Elsa.Scheduling.Contracts.IJob;

namespace Elsa.Modules.Quartz.Contracts;

public interface IElsaJobSerializer
{
    string Serialize(IElsaJob job);
    T Deserialize<T>(string json) where T : IElsaJob;
}