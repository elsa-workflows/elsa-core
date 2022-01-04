using System.Runtime.CompilerServices;
using Elsa.Management.Contracts;
using Elsa.Management.Models;
using Elsa.Management.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Management.Providers;

public class TypedTriggerProvider : ITriggerProvider
{
    private readonly ITriggerDescriber _triggerDescriber;
    private readonly ApiOptions _options;

    public TypedTriggerProvider(IOptions<ApiOptions> options, ITriggerDescriber triggerDescriber)
    {
        _triggerDescriber = triggerDescriber;
        _options = options.Value;
    }
        
    public async ValueTask<IEnumerable<TriggerDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var triggerTypes = _options.TriggerTypes;
        var descriptors = await DescribeTriggerTypesAsync(triggerTypes, cancellationToken).ToListAsync(cancellationToken);
        return descriptors;
    }
        
    public async IAsyncEnumerable<TriggerDescriptor> DescribeTriggerTypesAsync(IEnumerable<Type> triggerTypes, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var triggerType in triggerTypes)
        {
            var descriptor = await _triggerDescriber.DescribeTriggerAsync(triggerType, cancellationToken);
            yield return descriptor;
        }
    }
}