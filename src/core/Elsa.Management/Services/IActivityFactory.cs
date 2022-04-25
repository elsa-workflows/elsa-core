using Elsa.Management.Models;
using Elsa.Services;

namespace Elsa.Management.Services;

public interface IActivityFactory
{
    IActivity Create(Type type, ActivityConstructorContext context);
}