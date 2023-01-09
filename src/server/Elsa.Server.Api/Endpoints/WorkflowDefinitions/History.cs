using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowDefinitions;
using Elsa.Server.Api.Helpers;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-definitions/{definitionId}/history")]
    [Produces("application/json")]
    public class History : Controller
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly IMapper _mapper;
        private readonly ITenantAccessor _tenantAccessor;

        public History(IWorkflowDefinitionStore workflowDefinitionStore, IMapper mapper, ITenantAccessor tenantAccessor)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _mapper = mapper;
            _tenantAccessor = tenantAccessor;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<WorkflowDefinitionVersionModel>))]
        [SwaggerOperation(
            Summary = "Returns a list of workflow definition versions.",
            Description = "Returns a list of workflow definition versions.",
            OperationId = "WorkflowDefinitions.History",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<IActionResult> Handle(string definitionId, CancellationToken cancellationToken = default)
        {
            var tenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);
            var specification = GetSpecification(definitionId).And(new TenantSpecification<WorkflowDefinition>(tenantId));
            var definitions = await _workflowDefinitionStore.FindManyAsync(specification, new OrderBy<WorkflowDefinition>(x => x.Version, SortDirection.Descending), cancellationToken: cancellationToken);
            var versionModels = _mapper.Map<IList<WorkflowDefinitionVersionModel>>(definitions);

            var model = new
            {
                Items = versionModels
            };

            return Json(model, SerializationHelper.GetSettingsForWorkflowDefinition());
        }

        private Specification<WorkflowDefinition> GetSpecification(string definitionId) => new WorkflowDefinitionIdSpecification(definitionId, VersionOptions.All);
    }
}