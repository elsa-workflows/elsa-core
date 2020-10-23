using System.Threading;
using System.Threading.Tasks;
using Elsa.Triggers;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public class SignaledTrigger : ITrigger
    {
        public string Signal { get; set; }
    }

    public class SignaledTriggerProvider : TriggerProvider<SignaledTrigger, Signaled>
    {
        public override async ValueTask<ITrigger> GetTriggerAsync(TriggerProviderContext<Signaled> context, CancellationToken cancellationToken) =>
            new SignaledTrigger
            {
                Signal = await context.Activity.GetPropertyValueAsync(x => x.Signal, cancellationToken)
            };
    }
}