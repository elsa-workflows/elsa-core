using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowDefinitions;
using Elsa.Serialization;
using Elsa.Server.Api.Attributes;
using Elsa.Server.Api.Models;
using Elsa.Server.Api.Swagger.Examples;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-definitions")]
    [Produces("application/json")]
    public class GetMany : Controller
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly IContentSerializer _serializer;
        private readonly IMapper _mapper;

        public GetMany(IWorkflowDefinitionStore workflowDefinitionStore, IContentSerializer serializer, IMapper mapper)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _serializer = serializer;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ListModel<WorkflowDefinitionSummaryModel>))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowDefinitionPagedListExample))]
        [SwaggerOperation(
            Summary = "Returns a list of workflow definitions for the specified ids.",
            Description = "Returns a list of workflow definition summaries. When no version options are specified, the latest versions are returned.",
            OperationId = "WorkflowDefinitions.GetMany",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<ActionResult<WorkflowDefinitionSummaryModel[]>> Handle([RequiredFromQuery] string? ids, VersionOptions? version = default, CancellationToken cancellationToken = default)
        {
            IList<WorkflowDefinitionSummaryModel> summaries = new List<WorkflowDefinitionSummaryModel>();

            if (!string.IsNullOrWhiteSpace(ids))
            {
                version ??= VersionOptions.Latest;
                var splitIds = ids.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var specification = new ManyWorkflowDefinitionIdsSpecification(splitIds, version.Value);
                var items = await _workflowDefinitionStore.FindManyAsync(specification, cancellationToken: cancellationToken);
                summaries = _mapper.Map<IList<WorkflowDefinitionSummaryModel>>(items);
            }

            return Json(new ListModel<WorkflowDefinitionSummaryModel>(summaries), _serializer.GetSettings());
        }
    }
}