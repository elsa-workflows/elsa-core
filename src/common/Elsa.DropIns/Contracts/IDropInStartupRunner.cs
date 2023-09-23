using Elsa.DropIns.Core;

namespace Elsa.DropIns.Contracts;

public interface IDropInStartupRunner
{
    Task RunAsync(IEnumerable<IDropIn> startups, CancellationToken cancellationToken = default);
}