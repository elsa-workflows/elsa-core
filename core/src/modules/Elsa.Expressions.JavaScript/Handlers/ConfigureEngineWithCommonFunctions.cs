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
using Jint;
using Jint.Native;
using Jint.Runtime.Descriptors;
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

        // Add common functions. Each one is installed as a global whose value is produced the first time a script
        // reads the name, so a fresh engine no longer builds twenty-seven interop function wrappers for an
        // expression that calls none of them. The CLR delegate is created inside the factory for the same reason.
        AddFunction("getWorkflowDefinitionId", e => JsValue.FromObject(e, (Func<string>)(() => context.GetWorkflowExecutionContext().Workflow.Identity.DefinitionId)));
        AddFunction("getWorkflowDefinitionVersionId", e => JsValue.FromObject(e, (Func<string>)(() => context.GetWorkflowExecutionContext().Workflow.Identity.Id)));
        AddFunction("getWorkflowDefinitionVersion", e => JsValue.FromObject(e, (Func<int>)(() => context.GetWorkflowExecutionContext().Workflow.Identity.Version)));
        AddFunction("getWorkflowInstanceId", e => JsValue.FromObject(e, (Func<string>)(() => context.GetActivityExecutionContext().WorkflowExecutionContext.Id)));
        AddFunction("setCorrelationId", e => JsValue.FromObject(e, (Action<string?>)(value => context.GetActivityExecutionContext().WorkflowExecutionContext.CorrelationId = value)));
        AddFunction("getCorrelationId", e => JsValue.FromObject(e, (Func<string?>)(() => context.GetActivityExecutionContext().WorkflowExecutionContext.CorrelationId)));
        AddFunction("setWorkflowInstanceName", e => JsValue.FromObject(e, (Action<string?>)(value => context.GetWorkflowExecutionContext().Name = value)));
        AddFunction("getWorkflowInstanceName", e => JsValue.FromObject(e, (Func<string?>)(() => context.GetWorkflowExecutionContext().Name)));
        AddFunction("setVariable", e => JsValue.FromObject(e, (Action<string, object>)((name, value) =>
        {
            engine.SyncVariablesContainer(options, name, value);
            context.SetVariableInScope(name, value);
        })));
        AddFunction("getVariable", e => JsValue.FromObject(e, (Func<string, object?>)(name => context.GetVariableInScope(name))));
        AddFunction("getInput", e => JsValue.FromObject(e, (Func<string, object?>)(name => context.GetInput(name))));
        AddFunction("getOutputFrom", e => JsValue.FromObject(e, (Func<string, string?, object?>)((activityIdName, outputName) => context.GetOutput(activityIdName, outputName))));
        AddFunction("getLastResult", e => JsValue.FromObject(e, (Func<object?>)(() => context.GetLastResult())));
        AddFunction("isNullOrWhiteSpace", e => JsValue.FromObject(e, (Func<string, bool>)(value => string.IsNullOrWhiteSpace(value))));
        AddFunction("isNullOrEmpty", e => JsValue.FromObject(e, (Func<string, bool>)(value => string.IsNullOrEmpty(value))));
        AddFunction("toJson", e => JsValue.FromObject(e, (Func<object, string>)(value => Serialize(value))));
        AddFunction("parseGuid", e => JsValue.FromObject(e, (Func<string, Guid>)(value => Guid.Parse(value))));
        AddFunction("newGuid", e => JsValue.FromObject(e, (Func<Guid>)(() => Guid.NewGuid())));
        AddFunction("newGuidString", e => JsValue.FromObject(e, (Func<string>)(() => Guid.NewGuid().ToString())));
        AddFunction("newShortGuid", e => JsValue.FromObject(e, (Func<string>)(() => Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", ""))));
        AddFunction("bytesToString", e => JsValue.FromObject(e, (Func<byte[], string>)(value => Encoding.UTF8.GetString(value))));
        AddFunction("bytesFromString", e => JsValue.FromObject(e, (Func<string, byte[]>)(value => Encoding.UTF8.GetBytes(value))));
        AddFunction("bytesToBase64", e => JsValue.FromObject(e, (Func<byte[], string>)(value => Convert.ToBase64String(value))));
        AddFunction("bytesFromBase64", e => JsValue.FromObject(e, (Func<string, byte[]>)(value => Convert.FromBase64String(value))));
        AddFunction("stringToBase64", e => JsValue.FromObject(e, (Func<string, string>)(value => Convert.ToBase64String(Encoding.UTF8.GetBytes(value)))));
        AddFunction("stringFromBase64", e => JsValue.FromObject(e, (Func<string, string>)(value => Encoding.UTF8.GetString(Convert.FromBase64String(value)))));
        AddFunction("streamToBytes", e => JsValue.FromObject(e, (Func<Stream, byte[]>)(value => StreamToBytes(value))));
        AddFunction("streamToBase64", e => JsValue.FromObject(e, (Func<Stream, string>)(value => Convert.ToBase64String(StreamToBytes(value)))));

        // Deprecated, use newGuidString instead.
        AddFunction("getGuidString", e => JsValue.FromObject(e, (Func<string>)(() => Guid.NewGuid().ToString())));

        // Deprecated, use newShortGuid instead.
        AddFunction("getShortGuid", e => JsValue.FromObject(e, (Func<string>)(() => Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", ""))));
        return Task.CompletedTask;

        // AddLazyGlobal installs the property itself eagerly and defers only its value, so `in`, `hasOwnProperty`
        // and Object.getOwnPropertyNames see every function immediately and the factory runs at most once, on the
        // first read. NonEnumerable is the flag engine.SetValue(string, Delegate) applies, so the attributes — and
        // therefore what Object.keys(globalThis) reports — are exactly what they were.
        void AddFunction(string name, Func<Engine, JsValue> valueFactory) => engine.Advanced.AddLazyGlobal(name, valueFactory, PropertyFlag.NonEnumerable);
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

    private byte[] StreamToBytes(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}