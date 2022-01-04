using System.Text.Json;
using Elsa.Contracts;
using Elsa.Management.Contracts;
using Elsa.Management.Models;

namespace Elsa.Management.Services;

public class TriggerFactory : ITriggerFactory
{
    public ITrigger Create(Type type, TriggerConstructorContext context)
    {
        var trigger = (ITrigger)context.Element.Deserialize(type, context.SerializerOptions)!;
        return trigger;
    }
}