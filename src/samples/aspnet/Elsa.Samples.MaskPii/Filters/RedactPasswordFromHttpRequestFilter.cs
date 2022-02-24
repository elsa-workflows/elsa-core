using Elsa.DataMasking.Core.Abstractions;
using Elsa.DataMasking.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Samples.MaskPii.Filters;

/// <summary>
/// Redacts user passwords received on HTTP Endpoint activities.
/// </summary>
public class RedactPasswordFromHttpRequestFilter : HttpEndpointWorkflowJournalFilter
{
    protected override bool GetSupportsPath(WorkflowJournalFilterContext context, string path) => path == "/workflows/users/signup";

    protected override JToken ProcessBody(WorkflowJournalFilterContext context, JToken body)
    {
        body["password"] = JValue.CreateString("****");
        return body;
    }

    protected override JToken ProcessRawBody(WorkflowJournalFilterContext context, JToken rawBody)
    {
        // Parse the raw body string so we can access its password field as well. 
        var rawBodyModel = JObject.Parse(rawBody.Value<string>()!);
        rawBodyModel["Password"] = JValue.CreateString("****");

        // Update the rawBody field.
        return rawBodyModel.ToString(Formatting.Indented);
    }
}