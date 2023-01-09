using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;

namespace Elsa.Services.Triggers
{
    public class TriggerRemover: ITriggerRemover
    {
        private readonly ITriggerStore _triggerStore;

        public TriggerRemover(ITriggerStore triggerStore)
        {
            _triggerStore = triggerStore;
        }

        public async Task RemoveTriggerAsync(Trigger trigger)
        {
            await _triggerStore.DeleteAsync(trigger);
        }
    }
}