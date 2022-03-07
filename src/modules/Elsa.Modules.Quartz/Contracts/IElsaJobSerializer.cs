using Elsa.Jobs.Contracts;

namespace Elsa.Modules.Quartz.Contracts;

public interface IElsaJobSerializer
{
    string Serialize(IJob job);
    T Deserialize<T>(string json) where T : IJob;
}