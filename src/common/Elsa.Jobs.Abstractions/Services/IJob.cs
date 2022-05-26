using System.Threading.Tasks;
using Elsa.Jobs.Models;

namespace Elsa.Jobs.Services;

public interface IJob
{
    ValueTask ExecuteAsync(JobExecutionContext context);
}