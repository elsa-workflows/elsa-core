using System.Threading.Tasks;
using Elsa.Modules.Http.Models;

namespace Elsa.Modules.Http.Contracts;

/// <summary>
/// Implement this to control what to return to the client in case an unhandled exception occurs while executing the workflow.
/// </summary>
public interface IHttpEndpointWorkflowFaultHandler
{
    ValueTask HandleAsync(HttpEndpointFaultedWorkflowContext context);
}