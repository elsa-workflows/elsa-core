using System.Threading.Tasks;
using Elsa.DataMasking.Core.Contracts;
using Elsa.DataMasking.Core.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.DataMasking.Core.Abstractions;

/// <summary>
/// A base class for workflow journal filters for HTTP Endpoints.
/// </summary>
public abstract class HttpEndpointWorkflowJournalFilter : IWorkflowJournalFilter
{
    protected virtual ValueTask ProcessInboundRequestAsync(WorkflowJournalFilterContext context, JToken inboundRequest)
    {
        ProcessInboundRequest(context, inboundRequest);
        return new();
    }

    protected virtual void ProcessInboundRequest(WorkflowJournalFilterContext context, JToken inboundRequest)
    {
    }

    protected virtual ValueTask<bool> GetSupportsPathAsync(WorkflowJournalFilterContext context, string path) => new(GetSupportsPath(context, path));
    protected virtual bool GetSupportsPath(WorkflowJournalFilterContext context, string path) => true;
    protected virtual ValueTask<JToken> ProcessBodyAsync(WorkflowJournalFilterContext context, JToken body) => new(ProcessBody(context, body));
    protected virtual JToken ProcessBody(WorkflowJournalFilterContext context, JToken body) => body;
    protected virtual ValueTask<JToken> ProcessRawBodyAsync(WorkflowJournalFilterContext context, JToken rawBody) => new(ProcessRawBody(context, rawBody));
    protected virtual JToken ProcessRawBody(WorkflowJournalFilterContext context, JToken rawBody) => rawBody;
    protected virtual ValueTask<JToken> ProcessHeadersAsync(WorkflowJournalFilterContext context, JToken headers) => new(ProcessHeaders(context, headers));
    protected virtual JToken ProcessHeaders(WorkflowJournalFilterContext context, JToken headers) => headers;

    async ValueTask IWorkflowJournalFilter.ApplyAsync(WorkflowJournalFilterContext context) => await ApplyAsync(context);

    protected virtual async ValueTask ApplyAsync(WorkflowJournalFilterContext context)
    {
        // We're only interested in journal records related to the HttpEndpoint activity. 
        if (context.Record.ActivityType != "HttpEndpoint")
            return;

        // The HTTP endpoint receives user passwords - we do not want to store these in the workflow journal!
        var data = context.Record.Data;

        if (data == null)
            return;

        if (!data.TryGetValue("Inbound Request", out var inboundRequest))
            return;

        var path = inboundRequest["path"]!.Value<string>()!;

        if (!await GetSupportsPathAsync(context, path))
            return;

        await ProcessInboundRequestAsync(context, inboundRequest);
        inboundRequest["body"] = await ProcessBodyAsync(context, inboundRequest["body"]!);
        inboundRequest["rawBody"] = await ProcessRawBodyAsync(context, inboundRequest["rawBody"]!);
        inboundRequest["headers"] = await ProcessHeadersAsync(context, inboundRequest["headers"]!);
    }
}