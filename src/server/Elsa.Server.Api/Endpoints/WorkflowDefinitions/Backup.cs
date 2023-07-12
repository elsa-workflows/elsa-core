using System.Collections.Generic;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowDefinitions;
using Elsa.Persistence.Specifications;
using Elsa.Serialization;
using Elsa.Services;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Swashbuckle.AspNetCore.Annotations;
using System.IO.Compression;
using System.IO;
using System.Net.Http;

namespace Elsa.Server.Api.Endpoints.WorkflowDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-definitions/backup")]
    [Produces("application/zip")]
    public class Backup : ControllerBase
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly IContentSerializer _contentSerializer;
        private readonly ITenantAccessor _tenantAccessor;

        public Backup(IWorkflowDefinitionStore workflowDefinitionStore, IContentSerializer contentSerializer, ITenantAccessor tenantAccessor)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _contentSerializer = contentSerializer;
            _tenantAccessor = tenantAccessor;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Exports all or a selection of workflow definitions as JSON files contained within a zip archive download.",
            Description = "Exports workflow definitions as JSON files contained within a zip archive download.",
            OperationId = "WorkflowDefinitions.Backup",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task Handle(
            [FromQuery] string? ids,
            VersionOptions? version = default,
            CancellationToken cancellationToken = default)
        {
            Response.ContentType = "application/zip";
            Response.Headers.Add($"Content-Disposition", $"attachment; filename=\"Workflows-{DateTime.Now:yyyy-MM-dd-HH-mm}.zip\"");

            await Response.StartAsync(cancellationToken);

            var tenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);
            version ??= VersionOptions.Latest;
            var specification = List.GetSpecification(ids, version.Value).And(new TenantSpecification<WorkflowDefinition>(tenantId));

            var items = await _workflowDefinitionStore.FindManyAsync(specification, cancellationToken: cancellationToken);
            using var zipArchive = new ZipArchive(Response.BodyWriter.AsStream(), ZipArchiveMode.Create);
            {
                foreach (var workflowDefinition in items)
                {
                    var json = _contentSerializer.Serialize(workflowDefinition);
                    var bytes = Encoding.UTF8.GetBytes(json);
                    var hasWorkflowName = !string.IsNullOrWhiteSpace(workflowDefinition.Name);
                    var workflowName = hasWorkflowName ? workflowDefinition.Name!.Trim() : workflowDefinition.DefinitionId;

                    var fileName = hasWorkflowName
                        ? $"{workflowName.Underscore().Dasherize().ToLowerInvariant()}.json"
                        : $"workflow-definition-{workflowName.Underscore().Dasherize().ToLowerInvariant()}.json";

                    var zipEntry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);
                    await using var zFile = zipEntry.Open();
                    await zFile.WriteAsync(bytes, cancellationToken);
                }
            }
        }
    }
}