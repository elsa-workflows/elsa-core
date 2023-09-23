using System.Reflection;

namespace Elsa.DropIns.Contracts;

public interface IDropInStarter
{
    Task StartAsync(Assembly assembly, CancellationToken cancellationToken = default);
}