using System.Threading.Tasks;
using Elsa.Jobs.Models;

namespace Elsa.Jobs.Services;

public interface IJob
{
    string Id { get; set; }
    ValueTask ExecuteAsync(JobExecutionContext context);
}