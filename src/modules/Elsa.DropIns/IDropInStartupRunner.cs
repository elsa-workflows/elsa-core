using Elsa.DropIns.Core;

namespace Elsa.DropIns;

public interface IDropInStartupRunner
{
    Task RunAsync(IEnumerable<IDropInStartup> startups, CancellationToken cancellationToken = default);
}