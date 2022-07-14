using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Exceptions;

namespace Elsa.Activities.Http.Contracts;

/// <summary>
/// Implement this to control what to return to the client in case an unhandled exception occurs while executing the workflow.
/// For example, invalid input might cause a <see cref="CannotSetActivityPropertyValueException"/>, which could be due to invalid user input.
/// </summary>
public interface IHttpEndpointWorkflowFaultHandler
{
    ValueTask HandleAsync(HttpEndpointFaultedWorkflowContext context);
}