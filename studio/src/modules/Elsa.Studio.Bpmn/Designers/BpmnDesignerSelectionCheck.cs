using Elsa.Studio.Contracts;

namespace Elsa.Studio.Bpmn.Designers;

/// <summary>Resolves the BPMN designer when the host starts, so a host that enables designers it cannot choose between fails then.</summary>
internal sealed class BpmnDesignerSelectionCheck(BpmnDesignerResolver resolver) : IStartupTask
{
    public ValueTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        resolver.Resolve();
        return ValueTask.CompletedTask;
    }
}
