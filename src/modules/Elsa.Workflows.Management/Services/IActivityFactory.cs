using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Management.Services;

public interface IActivityFactory
{
    IActivity Create(Type type, ActivityConstructorContext context);
}