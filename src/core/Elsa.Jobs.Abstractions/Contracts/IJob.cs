using System.Threading.Tasks;
using Elsa.Jobs.Models;

namespace Elsa.Jobs.Contracts;

public interface IJob
{
    ValueTask ExecuteAsync(JobExecutionContext context);
}