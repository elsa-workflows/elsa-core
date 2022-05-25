using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Services;

public interface IActivityFactory
{
    IActivity Create(Type type, ActivityConstructorContext context);
}