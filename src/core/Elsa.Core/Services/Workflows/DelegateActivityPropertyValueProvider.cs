using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services.Workflows
{
    public class DelegateActivityPropertyValueProvider : IActivityPropertyValueProvider
    {
        public DelegateActivityPropertyValueProvider(Func<ActivityExecutionContext, ValueTask<object?>> valueProvider)
        {
            ValueProvider = valueProvider;
        }

        public Func<ActivityExecutionContext, ValueTask<object?>> ValueProvider { get; }

        public async ValueTask<object?> GetValueAsync(
            ActivityExecutionContext context,
            CancellationToken cancellationToken = default) =>
            await ValueProvider(context);
    }
}