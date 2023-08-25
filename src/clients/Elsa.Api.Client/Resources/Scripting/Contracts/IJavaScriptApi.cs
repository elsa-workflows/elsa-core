using Elsa.Api.Client.Resources.Scripting.Requests;
using JetBrains.Annotations;
using Refit;

namespace Elsa.Api.Client.Resources.Scripting.Contracts
{
    /// <summary>
    /// Represents a client for the workflow javascript API.
    /// </summary>
    [PublicAPI]
    public interface IJavaScriptApi
    {
        /// <summary>
        /// Dispatches a request to retrieve the type definitions of the specified workflow definition.
        /// </summary>
        /// <param name="definitionId">The definition ID of the workflow definition to retrieve the type definitions for.</param>
        /// <param name="request">Required request containing properties to retrieve the correct type definitions.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>A response containing the type definition data which can be used to extend auto completion in Monaco.</returns>
        [Post("/scripting/javascript/type-definitions/{definitionId}")]
        Task<HttpResponseMessage> GetTypeDefinitions(string definitionId, GetWorkflowJavaScriptDefinitionRequest request, CancellationToken cancellationToken);
    }
}
