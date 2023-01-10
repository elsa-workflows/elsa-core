using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface ITriggerRemover
    {
        Task RemoveTriggerAsync(Trigger trigger);
    }
}