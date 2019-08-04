using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Dashboard.Controllers
{
    [Area("elsa-dashboard")]
    [ApiController]
    [Route("/[area]/api/workflow-definitions")]
    [Produces("application/json")]
    public class WorkflowDefinitionsController : ControllerBase
    {
        private readonly IWorkflowDefinitionStore store;

        public WorkflowDefinitionsController(IWorkflowDefinitionStore store)
        {
            this.store = store;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var definitions = await store.ListAsync(VersionOptions.Latest);
            return Ok(definitions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var definition = await store.GetByIdAsync(id, VersionOptions.Latest, cancellationToken);
            return definition != null ? (IActionResult) Ok(definition) : NotFound();
        }

        [HttpPatch("{id}")]
        public virtual async Task<IActionResult> Patch(string id, JsonPatchDocument<WorkflowDefinition> patch, int? version = default,
            CancellationToken cancellationToken = default)
        {
            var versionOptions = version != null ? VersionOptions.SpecificVersion(version.Value) : VersionOptions.Latest;
            var document = await store.GetByIdAsync(id, versionOptions, cancellationToken);

            if (document == null)
                return NotFound();

            patch.ApplyTo(document, ModelState);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            document.Id = id;
            document = await store.UpdateAsync(document, cancellationToken);

            return Ok(document);
        }
    }
}