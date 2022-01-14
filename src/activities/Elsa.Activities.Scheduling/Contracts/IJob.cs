using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Scheduling.Contracts;

public interface IJob
{
    string JobId { get; }
}