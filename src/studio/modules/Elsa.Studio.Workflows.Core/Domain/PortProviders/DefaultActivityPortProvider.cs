using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Providers;

/// <summary>
/// A default implementation of <see cref="IActivityPortProvider"/> that returns the ports defined by the activity descriptor.
/// </summary>
public class DefaultActivityPortProvider : ActivityPortProviderBase
{
    /// <inheritdoc />
    public override double Priority => -1000;

    /// <inheritdoc />
    public override bool GetSupportsActivityType(PortProviderContext context) => true;

    /// <inheritdoc />
    public override IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        return context.ActivityDescriptor.Ports.ToList();
    }
}