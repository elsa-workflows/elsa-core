namespace Elsa.DropIns.Core;

/// <summary>
/// Implement this interface to run code wen the drop-in is added to the drop-in system at runtime.
/// </summary>
public interface IDropInStartup
{
    ValueTask StartAsync(CancellationToken cancellationToken = default);
}