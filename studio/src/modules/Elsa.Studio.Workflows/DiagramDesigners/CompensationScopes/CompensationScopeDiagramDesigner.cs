using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.DiagramDesigners.CompensationScopes;

public sealed class CompensationScopeDiagramDesigner : IDiagramDesigner
{
    private JsonObject _scope = [];
    private string? _documentBackendContextKey;
    private EventCallback _graphUpdatedCallback;

    public Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap)
    {
        _scope = activity;
        return Task.CompletedTask;
    }

    public async Task UpdateActivityAsync(string id, JsonObject activity)
    {
        bool updated;
        if (_scope.GetId() == id)
        {
            ReplaceContents(_scope, activity);
            updated = true;
        }
        else
        {
            updated = ReplaceChild(_scope, id, activity);
        }

        if (updated && _graphUpdatedCallback.HasDelegate)
        {
            await _graphUpdatedCallback.InvokeAsync();
        }
    }

    public Task UpdateActivityStatsAsync(string id, ActivityStats stats) => Task.CompletedTask;
    public Task SelectActivityAsync(string id) => Task.CompletedTask;
    public Task<JsonObject> ReadRootActivityAsync() => Task.FromResult(_scope);

    public RenderFragment DisplayDesigner(DisplayContext context)
    {
        _scope = context.Activity;
        _documentBackendContextKey = NativeCompensationDraftDocumentContext.GetBackendContextKey(context.WorkflowDefinition);
        _graphUpdatedCallback = context.GraphUpdatedCallback;
        return builder =>
        {
            builder.OpenComponent<CompensationScopeDesigner>(0);
            builder.AddAttribute(1, nameof(CompensationScopeDesigner.Activity), _scope);
            builder.AddAttribute(2, nameof(CompensationScopeDesigner.IsReadOnly), context.IsReadOnly);
            builder.AddAttribute(3, nameof(CompensationScopeDesigner.DocumentBackendContextKey), _documentBackendContextKey ?? "");
            builder.AddAttribute(4, nameof(CompensationScopeDesigner.ActivitySelected), context.ActivitySelectedCallback);
            builder.AddAttribute(5, nameof(CompensationScopeDesigner.GraphUpdated), context.GraphUpdatedCallback);
            builder.CloseComponent();
        };
    }

    private static bool ReplaceChild(JsonNode? node, string id, JsonObject replacement)
    {
        if (node is JsonObject obj)
        {
            if (obj.IsActivity() && string.Equals(obj.GetId(), id, StringComparison.Ordinal))
            {
                ReplaceContents(obj, replacement);
                return true;
            }

            foreach (var child in obj.Select(x => x.Value))
                if (ReplaceChild(child, id, replacement))
                    return true;
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array)
                if (ReplaceChild(child, id, replacement))
                    return true;
        }

        return false;
    }

    private static void ReplaceContents(JsonObject destination, JsonObject source)
    {
        var replacement = source.DeepClone().AsObject();
        destination.Clear();
        foreach (var property in replacement)
            destination[property.Key] = property.Value?.DeepClone();
    }
}
