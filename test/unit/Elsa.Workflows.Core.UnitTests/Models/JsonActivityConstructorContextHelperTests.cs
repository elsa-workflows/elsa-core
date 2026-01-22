
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Core.UnitTests.Models;

public sealed class JsonActivityConstructorContextHelperTests
{
    private static readonly JsonSerializerOptions _serializerOptions = new();

    [Theory]
    [InlineData("_Metadata")]
    [InlineData("Metadata")]
    [InlineData("_CustomProperties")]
    [InlineData("CustomProperties")]
    public void When_InputName_Is_ReservedKeyWord_Then_AddsException_WhenReadingInputs(string reservedInputName)
    {
        // Arrange
        var activityDescriptor = new ActivityDescriptor
        {
            Inputs =
            [
                new() 
                { 
                    Name = reservedInputName, 
                    Type = typeof(object),
                    IsSynthetic = true
                }
            ]
        };
        var jsonElement = GetJsonElementWithReservedInputName(reservedInputName);

        // Act
        var result = JsonActivityConstructorContextHelper.CreateActivity<WorkflowAsActivity>(
            activityDescriptor,
            jsonElement,
            _serializerOptions
        );

        // Assert
        Assert.Single(result.Exceptions, e => e is InvalidActivityDescriptorInputException);
    }

    private static JsonElement GetJsonElementWithReservedInputName(string inputName)
    {
        const string json = "{\"workflowDefinitionId\":\"Id\",\"id\":\"Id\",\"nodeId\":\"NodeId\",\"name\":\"Workflow-as-activity\",\"type\":\"WorkflowAsActivity\",\"customProperties\":{\"canStartWorkflow\":false,\"runAsynchronously\":false},\"metadata\":{\"designer\":{\"position\":{\"x\":1525.99609375,\"y\":171.5625},\"size\":{\"width\":230.4296875,\"height\":68.4375}},\"displayText\":\"Test\"},\"<inputName>\":{\"typeName\":\"Object\",\"expression\":{\"type\":\"JavaScript\",\"value\":\"return {\\n    \\u0022key\\u0022 : obj.value\\n}\"},\"memoryReference\":{\"id\":\"memref:input--name\"}}}";
        var jsonWithInputName = json.Replace("<inputName>", inputName);
        return JsonElement.Parse(jsonWithInputName);
    }

    private class WorkflowAsActivity : IActivity
    {
        public string Id { get; set; } = "Id";
        public string NodeId { get; set; } = "NodeId";
        public string? Name { get; set; } = "Workflow-as-activity";
        public string Type { get; set; } = "WorkflowAsActivity";
        public int Version { get; set; } = 1;

        [JsonIgnore]
        public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
        public IDictionary<string, object> SyntheticProperties { get; set; } = new Dictionary<string, object>();

        [JsonIgnore]
        public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
        {
            return new(true);
        }

        public ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            return new();
        }
    }
}
