using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Scripting.JavaScript.Models;
using Elsa.Scripting.JavaScript.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.Scripting.JavaScript.TypeDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/scripting/javascript/type-definitions/{workflowDefinitionId}")]
    [Produces("application/json")]
    public class Get : Controller
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly ITypeScriptDefinitionService _typeScriptDefinitionService;

        public Get(IWorkflowDefinitionStore workflowDefinitionStore, ITypeScriptDefinitionService typeScriptDefinitionService)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _typeScriptDefinitionService = typeScriptDefinitionService;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileContentResult))]
        [SwaggerOperation(
            Summary = "Returns a TypeScript type definition file generated based on the specified workflow definition.",
            Description = "Returns a TypeScript type definition file generated based on the specified workflow definition.",
            OperationId = "JavaScriptLanguageServices.GetTypeDefinitions",
            Tags = new[] { "JavaScriptLanguageServices" })
        ]
        public async Task<IActionResult> Handle(string workflowDefinitionId, IntellisenseContext? context = default, VersionOptions? version = default, CancellationToken cancellationToken = default)
        {
            version ??= VersionOptions.Latest;
            var workflowDefinition = await _workflowDefinitionStore.FindByDefinitionIdAsync(workflowDefinitionId, version.Value, cancellationToken);
            var typeDefinitions = await _typeScriptDefinitionService.GenerateTypeScriptDefinitionsAsync(workflowDefinition, context, cancellationToken);
            var fileName = $"elsa.{workflowDefinitionId}.d.ts";
            var data = Encoding.UTF8.GetBytes(typeDefinitions);
            return File(data, "application/x-typescript", fileName);
        }
    }
}