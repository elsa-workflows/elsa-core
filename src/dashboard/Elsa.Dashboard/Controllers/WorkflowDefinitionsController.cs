using System.Threading;
using System.Threading.Tasks;
using Elsa.Dashboard.Microsoft;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
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
        private readonly IWorkflowPublisher publisher;
        private readonly IIdGenerator idGenerator;

        public WorkflowDefinitionsController(
            IWorkflowDefinitionStore store, 
            IWorkflowPublisher publisher, 
            IIdGenerator idGenerator)
        {
            this.store = store;
            this.publisher = publisher;
            this.idGenerator = idGenerator;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var definitions = await store.ListAsync(VersionOptions.Latest);
            return Ok(definitions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, int? version = default, CancellationToken cancellationToken = default)
        {
            var versionOptions = version != null ? VersionOptions.SpecificVersion(version.Value) : VersionOptions.Latest;
            var definition = await store.GetByIdAsync(id, versionOptions, cancellationToken);
            return definition != null ? (IActionResult) Ok(definition) : NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Post(WorkflowDefinition model, CancellationToken cancellationToken = default)
        {
            model.Id = idGenerator.Generate();
            model.Version = 1;
            model.IsPublished = false;
            model.IsLatest = true;
            
            await store.AddAsync(model, cancellationToken);
            return Ok(model);
        }
        
        [HttpPost("{id}/publish")]
        public async Task<IActionResult> Publish(string id, CancellationToken cancellationToken = default)
        {
            var definition = await publisher.PublishAsync(id, cancellationToken);
            return definition != null ? (IActionResult) Ok(definition) : NotFound();
        }
        
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id, JsonPatchDocument<WorkflowDefinition> patch, CancellationToken cancellationToken = default)
        {
            var definition = await publisher.GetDraftAsync(id, cancellationToken);

            if (definition == null)
                return NotFound();
            
            patch.CustomApplyTo(definition, ModelState);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            definition.Id = id;
            definition = await store.UpdateAsync(definition, cancellationToken);
            
            return Ok(definition);
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken = default)
        {
            var count = await store.DeleteAsync(id, cancellationToken);
            return count > 0 ? (IActionResult) NoContent() : NotFound();
        }
    }
}