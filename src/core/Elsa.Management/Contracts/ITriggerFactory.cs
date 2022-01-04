using Elsa.Contracts;
using Elsa.Management.Models;

namespace Elsa.Management.Contracts;

public interface ITriggerFactory
{
    ITrigger Create(Type type, TriggerConstructorContext context);
}