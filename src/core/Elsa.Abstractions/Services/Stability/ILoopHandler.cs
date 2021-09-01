using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services.Stability
{
    public interface ILoopHandler
    {
        ValueTask HandleLoop(ActivityExecutionContext context);
    }
}