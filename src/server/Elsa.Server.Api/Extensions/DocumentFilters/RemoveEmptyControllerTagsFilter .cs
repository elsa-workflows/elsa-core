#if NET10_0_OR_GREATER
using Microsoft.OpenApi;
#else
using Microsoft.OpenApi.Models;
#endif
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace Elsa.Server.Api.Extensions.DocumentFilters
{
    /// <summary>
    /// Removes any top-level tag definitions from the OpenAPI document that aren't actually assigned to any endpoint operations.
    /// This helps to keep the generated documentation clean and focused on relevant tags.
    /// </summary>
    /// /// <remarks>
    /// <para>
    /// <b>Root Cause:</b> Swashbuckle's annotation engine bypasses its custom tag selection rules 
    /// when using <c>[SwaggerOperation(Tags = ...)]</c>. This leaves behind "ghost" controller-named tags 
    /// at the root level of the generated OpenAPI document that contain zero active endpoints.
    /// See Swashbuckle Issue #2618 (https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2618).
    /// </para>
    /// <para>
    /// <b>.NET 10 Conflict:</b> In .NET 10, the native <c>Microsoft.AspNetCore.OpenApi</c> pipeline 
    /// implicitly pushes default metadata tags down the <c>ApiExplorer</c> framework prior to Swashbuckle 
    /// evaluation. This renders internal interception strategies like <c>TagActionsBy</c> ineffective.
    /// </para>
    /// <para>
    /// <b>Resolution:</b> This filter executes exactly once per document build. It scans all actively mapped 
    /// operations, compiles a list of utilized tags, and purges any unmapped root-level tag headers to preserve 
    /// clean layout grouping in the Swagger UI.
    /// </para>
    /// </remarks>
    public class RemoveEmptyControllerTagsFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // 1. Find all tags that are actually assigned to at least one endpoint operation
#if NET10_0_OR_GREATER
            // In .NET 10, operation.Tags is ISet<OpenApiTagReference>
            var activeTags = swaggerDoc.Paths.Values
                .SelectMany(path => path.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>())
                .SelectMany(op => op.Tags?.Select(t => t.Reference?.Id) ?? Enumerable.Empty<string?>())
                .Where(id => !string.IsNullOrEmpty(id))
                .ToHashSet();
#else
            // In .NET 8/9, operation.Tags is IList<OpenApiTag>
            var activeTags = swaggerDoc.Paths.Values
                .SelectMany(path => path.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>())
                .SelectMany(op => op.Tags?.Select(t => t.Name) ?? Enumerable.Empty<string?>())
                .Where(name => !string.IsNullOrEmpty(name))
                .ToHashSet();
#endif

            // 2. Remove any top-level tag definitions that aren't actively used by an operation
            if (swaggerDoc.Tags != null)
            {
#if NET10_0_OR_GREATER
                // In .NET 10, swaggerDoc.Tags is ISet<OpenApiTag>
                var tagsToRemove = swaggerDoc.Tags.Where(tag => !activeTags.Contains(tag.Name)).ToList();
                foreach (var tag in tagsToRemove)
                {
                    swaggerDoc.Tags.Remove(tag);
                }
#else
                // In .NET 8/9, swaggerDoc.Tags is IList<OpenApiTag>
                swaggerDoc.Tags = swaggerDoc.Tags
                    .Where(tag => activeTags.Contains(tag.Name))
                    .ToList();
#endif
            }
        }
    }
}