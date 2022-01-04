using Elsa.Contracts;
using Elsa.Management.Models;

namespace Elsa.Management.Contracts;

public interface IActivityFactory
{
    IActivity Create(Type type, ActivityConstructorContext context);
}