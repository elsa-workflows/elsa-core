using Elsa.Extensions;
using Elsa.Http;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.Memory;
using Json.Path;
using Microsoft.AspNetCore.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Trimble.Elsa.Activities.Activities;

/// <summary>
/// Makes an HTTP call and maps the result to variables based upon the JsonPath
/// supplied in the OutputVariableMap.
/// </summary>
[Activity(
    "Trimble.Elsa.Activities.Activities",
    "ServiceRegistry",
    Description = "Send an HTTP request and set output variables.",
    DisplayName = "HTTP Request Parser",
    Kind        = ActivityKind.Task)]
public class SendHttpRequestMapper : FlowSendHttpRequest
{
    private Input<string?> _originalAuthorization = default!;
    private Input<object?> _originalContent = default!;

    /// <summary>
    /// Primary constructor.
    /// </summary>
    public SendHttpRequestMapper(
        EncryptedVariableString? authorization,
        string content,
        List<int> expectedStatusCodes,
        Dictionary<string, string[]> headers,
        string method,
        List<ResponseMapRecord> outputVariableMap,
        Variable<string> parsedContent,
        Variable<int> statusCode,
        Variable<string> url,
        [CallerFilePath] string? source = default,
        [CallerLineNumber] int? line = default) : base(source, line)
    {
        // If authorization is null the base class constructor in SendHttpRequestBase
        // handles its (ugly) creation.
        if (authorization is not null)
        {
            Authorization = new(authorization);
        }

        Content = new(content);
        ContentType = new("application/json");
        ExpectedStatusCodes = new(expectedStatusCodes);
        RequestHeaders = new(new HttpHeaders(headers));
        Method = new(method);
        ParsedContent = new(parsedContent);
        StatusCode = new(statusCode);
        Url = new(url);

        _originalAuthorization = Authorization;
        _originalContent = Content;

        OutputVariableMap = outputVariableMap;
    }

    /// <summary>
    /// Default constructor used for serdes.
    /// </summary>
    public SendHttpRequestMapper(
        [CallerFilePath] string? source = default,
        [CallerLineNumber] int? line = default) : base(source, line) {}

    /// <summary>
    /// The map that determines what fields on the response should correspond
    /// to which data variables as the capability instance understands them.
    ///
    /// I.e. enterpriseId field on the response should map to
    /// enterprise~1:enterprise:id variable on the instance.
    /// </summary>
    public List<ResponseMapRecord> OutputVariableMap { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        context.LogInfo<SendHttpRequestMapper>(
            "Executing SendHttpRequestMapper",
            new { URI = Url.Get(context) });

        // Add trace ID to headers if its not already there.
        var headers = context.Get(RequestHeaders) ?? [];
        if (!headers.Keys.Any(key => key.Equals("X-Trace-Id", StringComparison.InvariantCultureIgnoreCase)))
        {
            var traceId = context.WorkflowExecutionContext.CorrelationId
                ?? context.WorkflowExecutionContext.Workflow.Id;
            headers.Add("X-Trace-Id", [ traceId ]);
        }
        RequestHeaders = new(headers);

        context.LogInfo<SendHttpRequestMapper>(
            "About to variable replace Auth and Content",
            new
            {
                URI     = Url.Get(context),
                Method  = Method.Get(context),
                Content = Content.Get(context)
            });

        DecryptContent(context);
        DecryptAuthorization(context);

        context.LogInfo<SendHttpRequestMapper>(
            "About to make HTTP(S) request.",
            new
            {
                URI     = Url.Get(context),
                Method  = Method.Get(context),
                Content = Content.Get(context)
            });

        await base.ExecuteAsync(context);

        // Having made the call with decrypted authorization and content
        // return both properties to their original values.
        Content = _originalContent;
        Authorization = _originalAuthorization;
    }

    /// <summary>
    /// Decrypt the Authorization property after saving its original value
    /// for later restoration.
    private void DecryptAuthorization(ActivityExecutionContext context)
    {
        if (Authorization is null)
        {
            return;
        }
        _originalAuthorization = Authorization;

        var decrypted = Authorization
            .Decrypt(context)?
            .ReplaceTokens(context);

        Authorization = new(decrypted);
    }

    /// <summary>
    /// Decrypt Content and save its original value for later restoration.
    /// </summary>
    private void DecryptContent(ActivityExecutionContext context)
    {
        if (Content is null)
        {
            return;
        }
        _originalContent = Content;

        var decrypted = Content.Decrypt(context);
        if (decrypted.Expression?.Value is null)
        {
            return;
        }

        Content = decrypted.Expression.Value switch
        {
            Dictionary<string, string> dict => new(dict.ReplaceTokens(context)),
            string str => new(str.ReplaceTokens(context)),
            _ => Content
        };
    }

    /// <inheritdoc />
    protected override async ValueTask HandleResponseAsync(
        ActivityExecutionContext context,
        HttpResponseMessage response)
    {
        await base.HandleResponseAsync(context, response);

        // After allowing the base class to do its work in terms of handling the
        // response we now tackle the purpose of this class: to map the output
        // variables of this activity onto the "namespaced variables" as this
        // service understands them.
        //
        // For example an output variable from this task may be a field on the
        // JSON response from the HTTP call called "accountId" and we need to
        // write this into a variable called "cdh-account~1:account:account-id".
        // If there is nothing to map then we are already done.
        if (OutputVariableMap is null)
        {
            return;
        }

        var expressionExecCtx = context.ExpressionExecutionContext.Get(ParsedContent);

        // 520905: Will not handle nested dictionaries because they will 
        // serialize into KeyValue pairs rather than the original JSON schema.
        // There may be a way to retain this as a JSON string by re-implementing
        // SendHttpRequestBase::ParseContentAsync(ActivityExecutionContext context, HttpContent httpContent)
        if (expressionExecCtx is IDictionary<string, object?> dict)
        {
            expressionExecCtx = JsonSerializer.Serialize(dict);
        }

        // Faults the workflow if an expected status code is not present
        // This is also implemented in the workflow to route to a Fault 
        // activity, but by throwing an exception here, a more descriptive 
        // error message may be returned.
        var expectedStatusCodes = ExpectedStatusCodes.Get(context) ?? [200];
        if (expectedStatusCodes.Any() &&
            response is not null &&
            !expectedStatusCodes.Contains((int)response.StatusCode))
        {
            context.LogCritical<SendHttpRequestMapper>(
                $"The call to {response.RequestMessage?.RequestUri} {response.RequestMessage?.Method} returned an unexpected status code of {response.StatusCode}",
                expressionExecCtx);
        }

        foreach (var map in OutputVariableMap)
        {
            object? payloadValue = string.Empty;
            if (expressionExecCtx is string json)
            {
                var jsonNodes = JsonNode.Parse(json);
                var jsonPath = JsonPath.Parse(map.JsonPath);
                var pathResult = jsonPath.Evaluate(jsonNodes);

                // If payload is an integer, there is a fail in serializing
                // the workflow to the data store in the PolymorphicObjectConverter 
                // See https://dev.azure.com/ViewpointVSO/Shared%20Services/_workitems/edit/552815
                // This may have improved in 3.2 and should be tested.
                // For now, we will revert all variable to strings
                payloadValue = pathResult?.Matches?.FirstOrDefault()?.Value?.ToString();
            }

            context.LogInfo<SendHttpRequestMapper>(
                "About to map HTTP response to variable",
                new
                {
                    Path = map.JsonPath,
                    Variable = map.VariableName,
                    ValueFromPath = payloadValue
                });

            if (string.IsNullOrEmpty(payloadValue?.ToString()))
            {
                context.LogError<SendHttpRequestMapper>(
                    $"JSON path was not found in response.",
                    new
                    {
                        Response = expressionExecCtx,
                        StatusCode = response?.StatusCode,
                        URI = response?.RequestMessage?.RequestUri,
                        Path = map.JsonPath
                    });
                return;
            }

            payloadValue = context.EncryptIfNeeded(map.VariableName, payloadValue);
            context.SetVariable(map.VariableName, payloadValue);

            context.LogDebug<SendHttpRequestMapper>(
                "Obtained HTTP response value",
                new { value = payloadValue });
        }
    }

    /// <inheritdoc />
    protected override ValueTask HandleRequestExceptionAsync(
        ActivityExecutionContext context,
        HttpRequestException exception)
    {
        context.LogCritical<SendHttpRequestMapper>(
           "Exception in SendHttpRequestMapper",
           new { URI = Url.Get(context) },
           exception);

        return base.HandleRequestExceptionAsync(context, exception);
    }

    /// <inheritdoc />
    protected override ValueTask HandleTaskCanceledExceptionAsync(
        ActivityExecutionContext context,
        TaskCanceledException exception)
    {
        // The default behavior for the FlowSendHttpRequest base class is to
        // continue the workflow and set an outcome of "Timeout" that can be
        // handled by a dedicated activity. We are choosing to immediately fault
        // all workflows by throwing an exception to avoid unassigned data variables.
        context.LogCritical<SendHttpRequestMapper>(
            "SendHttpRequestMapper activity canceled, probably due to a timeout (100 seconds). The workflow should be re-run.",
            new
            {
                URI     = Url.Get(context),
                Method  = Method.Get(context),
                Content = Content.Get(context)
            },
            exception);

        return ValueTask.CompletedTask;
    }
}
