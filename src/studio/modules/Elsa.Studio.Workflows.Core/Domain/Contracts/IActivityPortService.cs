using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contexts;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Defines the contract for activity port service.
/// </summary>
public interface IActivityPortService
{
    IActivityPortProvider GetProvider(PortProviderContext context);
    IEnumerable<Port> GetPorts(PortProviderContext context);
}