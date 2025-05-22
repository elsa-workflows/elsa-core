using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Elsa.Extensions;
using Elsa.Expressions.JavaScript.Helpers;
using Elsa.Expressions.JavaScript.Notifications;
using Elsa.Expressions.JavaScript.Options;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Expressions.JavaScript.Handlers;

/// <summary>
/// A handler that configures the Jint engine with common functions.
/// </summary>
[UsedImplicitly]
public class ConfigureEngineWithCommonFunctions(IOptions<JintOptions> options) : INotificationHandler<EvaluatingJavaScript>
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = CreateJsonSerializerOptions();

    /// <inheritdoc />
    public Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        var engine = notification.Engine;
        var context = notification.Context;
        
        // Add common functions.
        engine.SetValue("getWorkflowDefinitionId", (Func<string>)(() => context.GetWorkflowExecutionContext().Workflow.Identity.DefinitionId));
        engine.SetValue("getWorkflowDefinitionVersionId", (Func<string>)(() => context.GetWorkflowExecutionContext().Workflow.Identity.Id));
        engine.SetValue("getWorkflowDefinitionVersion", (Func<int>)(() => context.GetWorkflowExecutionContext().Workflow.Identity.Version));
        engine.SetValue("getWorkflowInstanceId", (Func<string>)(() => context.GetActivityExecutionContext().WorkflowExecutionContext.Id));
        engine.SetValue("setCorrelationId", (Action<string?>)(value => context.GetActivityExecutionContext().WorkflowExecutionContext.CorrelationId = value));
        engine.SetValue("getCorrelationId", (Func<string?>)(() => context.GetActivityExecutionContext().WorkflowExecutionContext.CorrelationId));
        engine.SetValue("setWorkflowInstanceName", (Action<string?>)(value => context.GetWorkflowExecutionContext().Name = value));
        engine.SetValue("getWorkflowInstanceName", (Func<string?>)(() => context.GetWorkflowExecutionContext().Name));
        engine.SetValue("setVariable", (Action<string, object>)((name, value) =>
        {
            engine.SyncVariablesContainer(options, name, value);
            context.SetVariableInScope(name, value);
        }));
        engine.SetValue("getVariable", (Func<string, object?>)(name => context.GetVariableInScope(name)));
        engine.SetValue("getInput", (Func<string, object?>)(name => context.GetInput(name)));
        engine.SetValue("getOutputFrom", (Func<string, string?, object?>)((activityIdName, outputName) => context.GetOutput(activityIdName, outputName)));
        engine.SetValue("getLastResult", (Func<object?>)(() => context.GetLastResult()));
        engine.SetValue("isNullOrWhiteSpace", (Func<string, bool>)(value => string.IsNullOrWhiteSpace(value)));
        engine.SetValue("isNullOrEmpty", (Func<string, bool>)(value => string.IsNullOrEmpty(value)));
        engine.SetValue("toJson", (Func<object, string>)(value => Serialize(value)));
        engine.SetValue("parseGuid", (Func<string, Guid>)(value => Guid.Parse(value)));
        engine.SetValue("newGuid", (Func<Guid>)(() => Guid.NewGuid()));
        engine.SetValue("newGuidString", (Func<string>)(() => Guid.NewGuid().ToString()));
        engine.SetValue("newShortGuid", (Func<string>)(() => Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "")));
        engine.SetValue("bytesToString", (Func<byte[], string>)(value => Encoding.UTF8.GetString(value)));
        engine.SetValue("bytesFromString", (Func<string, byte[]>)(value => Encoding.UTF8.GetBytes(value)));
        engine.SetValue("bytesToBase64", (Func<byte[], string>)(value => Convert.ToBase64String(value)));
        engine.SetValue("bytesFromBase64", (Func<string, byte[]>)(value => Convert.FromBase64String(value)));
        engine.SetValue("stringToBase64", (Func<string, string>)(value =>  Convert.ToBase64String(Encoding.UTF8.GetBytes(value))));
        engine.SetValue("stringFromBase64", (Func<string, string>)(value => Encoding.UTF8.GetString(Convert.FromBase64String(value))));
        
        // Deprecated, use newGuidString instead.
        engine.SetValue("getGuidString", (Func<string>)(() => Guid.NewGuid().ToString()));

        // Deprecated, use newShortGuid instead.
        engine.SetValue("getShortGuid", (Func<string>)(() => Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "")));
        return Task.CompletedTask;
    }
    
    private string Serialize(object value)
    {
        return JsonSerializer.Serialize(value, _jsonSerializerOptions);
    }
    
    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}