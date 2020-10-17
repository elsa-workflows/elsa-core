using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Triggers
{
    public interface ITriggerProvider
    {
        Type ForType();
        Type ForActivityType();
        ValueTask<ITrigger> GetTriggerAsync(TriggerProviderContext context, CancellationToken cancellationToken = default);
    }
}