using System.Runtime.CompilerServices;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Http.Options;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Open.Linq.AsyncExtensions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Elsa.Activities.Http.OpenApi;

/// <summary>
/// Adds additional <see cref="OpenApiPathItem"/>s to the <see cref="OpenApiDocument"/> based on available <see cref="HttpEndpoint"/> activities used as starting points found in workflows.
/// </summary>
public class HttpEndpointDocumentFilter : IDocumentFilter
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISchemaGenerator _schemaGenerator;
    private readonly HttpActivityOptions _options;

    public HttpEndpointDocumentFilter(IServiceScopeFactory scopeFactory, ISchemaGenerator schemaGenerator, IOptions<HttpActivityOptions> options)
    {
        _scopeFactory = scopeFactory;
        _schemaGenerator = schemaGenerator;
        _options = options.Value;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags ??= new List<OpenApiTag>();

        var tag = new OpenApiTag
        {
            Name = "Workflow Endpoints",
            Description = "A collection of HTTP endpoints exposed by workflows",
        };

        var schemaRepository = context.SchemaRepository;

        swaggerDoc.Tags.Add(tag);

        var httpEndpoints = FindHttpEndpointsAsync(CancellationToken.None).ToLookupAsync(x => x.Path).Result;

        foreach (var grouping in httpEndpoints)
        {
            var path = _options.BasePath?.Add(grouping.Key) ?? grouping.Key;
            var first = grouping.First();

            swaggerDoc.Paths.Add(path, new OpenApiPathItem
            {
                Description = first.Description,
                Operations = grouping.ToDictionary(GetOperationType, httpEndpoint =>
                {
                    var operation = new OpenApiOperation
                    {
                        Tags = { tag },
                    };

                    if (httpEndpoint.TargetType != null || httpEndpoint.JsonSchema != null)
                    {
                        operation.RequestBody = new OpenApiRequestBody
                        {
                            Required = true,
                            Content =
                            {
                                ["Unspecified"] = new OpenApiMediaType
                                {
                                    Schema = httpEndpoint.TargetType != null ? _schemaGenerator.GenerateSchema(httpEndpoint.TargetType, schemaRepository) : new OpenApiSchema() // TODO: Convert JSON schema into OpenAPI schema.
                                }
                            }
                        };
                    }

                    return operation;
                })
            });
        }
    }

    private async IAsyncEnumerable<HttpEndpointDescriptor> FindHttpEndpointsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var triggerFinder = scope.ServiceProvider.GetRequiredService<ITriggerFinder>();
        var workflowRegistry = scope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();
        var triggerExtractor = scope.ServiceProvider.GetRequiredService<IGetsTriggersForWorkflowBlueprints>();
        var workflowExecutionContextFactory = scope.ServiceProvider.GetRequiredService<ICreatesWorkflowExecutionContextForWorkflowBlueprint>();
        var activityExecutionContextFactory = scope.ServiceProvider.GetRequiredService<ICreatesActivityExecutionContextForActivityBlueprint>();
        var bookmarks = await triggerFinder.FindTriggersByTypeAsync<HttpEndpointBookmark>(cancellationToken: cancellationToken).ToList();
        var workflowDefinitionIds = bookmarks.Select(x => x.WorkflowDefinitionId).Distinct().ToList();
        var workflowBlueprints = await workflowRegistry.FindManyByDefinitionIds(workflowDefinitionIds, VersionOptions.Published, cancellationToken);

        foreach (var workflowBlueprint in workflowBlueprints)
        {
            var workflowTriggers = await triggerExtractor.GetTriggersAsync(workflowBlueprint, cancellationToken);
            var workflowExecutionContext = await workflowExecutionContextFactory.CreateWorkflowExecutionContextAsync(workflowBlueprint, cancellationToken);

            foreach (var workflowTrigger in workflowTriggers)
            {
                if (workflowTrigger.Bookmark is HttpEndpointBookmark)
                    yield return await GetHttpEndpointDescriptor(workflowExecutionContext, workflowTrigger, activityExecutionContextFactory);
            }
        }
    }

    private async Task<HttpEndpointDescriptor> GetHttpEndpointDescriptor(
        WorkflowExecutionContext workflowExecutionContext,
        WorkflowTrigger workflowTrigger,
        ICreatesActivityExecutionContextForActivityBlueprint activityExecutionContextFactory)
    {
        var activityId = workflowTrigger.ActivityId;
        var httpEndpointActivity = workflowExecutionContext.WorkflowBlueprint.GetActivity(activityId)!;
        var httpEndpointBookmark = (HttpEndpointBookmark) workflowTrigger.Bookmark;
        var activityExecutionContext = activityExecutionContextFactory.CreateActivityExecutionContext(httpEndpointActivity, workflowExecutionContext, CancellationToken.None);
        var httpEndpointActivityAccessor = new ActivityBlueprintWrapper<HttpEndpoint>(activityExecutionContext);
        var displayName = httpEndpointActivity.DisplayName;
        var description = httpEndpointActivity.Description;
        var path = httpEndpointBookmark.Path;
        var method = httpEndpointBookmark.Method!;
        var targetType = await httpEndpointActivityAccessor.EvaluatePropertyValueAsync(x => x.TargetType);
        var schema = await httpEndpointActivityAccessor.EvaluatePropertyValueAsync(x => x.Schema);

        return new HttpEndpointDescriptor(activityId, path, method, displayName, description, targetType, schema);
    }

    private OperationType GetOperationType(HttpEndpointDescriptor httpEndpoint) =>
        httpEndpoint.Method switch
        {
            { } s when s.Equals(HttpMethods.Get, StringComparison.OrdinalIgnoreCase) => OperationType.Get,
            { } s when s.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase) => OperationType.Post,
            { } s when s.Equals(HttpMethods.Patch, StringComparison.OrdinalIgnoreCase) => OperationType.Patch,
            { } s when s.Equals(HttpMethods.Delete, StringComparison.OrdinalIgnoreCase) => OperationType.Delete,
            { } s when s.Equals(HttpMethods.Put, StringComparison.OrdinalIgnoreCase) => OperationType.Patch,
            { } s when s.Equals(HttpMethods.Options, StringComparison.OrdinalIgnoreCase) => OperationType.Options,
            { } s when s.Equals(HttpMethods.Head, StringComparison.OrdinalIgnoreCase) => OperationType.Head,
            { } s when s.Equals(HttpMethods.Trace, StringComparison.OrdinalIgnoreCase) => OperationType.Trace,
            _ => OperationType.Get
        };
}

internal record HttpEndpointDescriptor(string Id, string Path, string Method, string? DisplayName, string? Description, Type? TargetType, string? JsonSchema);