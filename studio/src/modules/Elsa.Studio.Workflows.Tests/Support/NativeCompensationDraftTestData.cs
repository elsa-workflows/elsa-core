using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Tests.Support;

internal static class NativeCompensationDraftTestData
{
    public static JsonObject CreateLegacySetVariableWithScopeLikeLiteralAndMetadata() => JsonNode.Parse(
        """
        {
          "id": "set-variable-1",
          "type": "Elsa.SetVariable",
          "version": 1,
          "variable": { "id": "payload-variable", "name": "Payload", "typeName": "Object" },
          "value": {
            "typeName": "Object",
            "expression": {
              "type": "Literal",
              "value": {
                "type": "Elsa.CompensationScope",
                "version": 1,
                "draftSchemaVersion": 1,
                "steps": []
              }
            }
          },
          "metadata": {
            "designer": {
              "payload": {
                "type": "Elsa.CompensationScope",
                "version": 1,
                "draftSchemaVersion": 1,
                "steps": []
              }
            }
          }
        }
        """)!.AsObject();

    public static JsonObject CreateOrdinarySetVariableWithScopeLikeUserData() => JsonNode.Parse(
        """
        {
          "id": "set-variable-1",
          "nodeId": "set-variable-1",
          "type": "Elsa.SetVariable",
          "version": 1,
          "variable": {
            "id": "payload-variable",
            "name": "Payload",
            "typeName": "Object",
            "storageDriverTypeName": "Elsa.Workflows.Services.WorkflowStorageDriver, Elsa.Workflows.Core"
          },
          "value": {
            "typeName": "Object",
            "expression": {
              "type": "Literal",
              "value": {
                "type": "Elsa.CompensationScope",
                "version": 1,
                "draftSchemaVersion": 1,
                "steps": []
              }
            }
          },
          "payload": {
            "type": "Elsa.CompensationScope",
            "version": 1,
            "draftSchemaVersion": 1,
            "steps": []
          },
          "metadata": {
            "designer": { "position": { "x": 10, "y": 20 } },
            "futureActivityData": {
              "payload": {
                "type": "Elsa.CompensationScope",
                "version": 1,
                "draftSchemaVersion": 1,
                "steps": []
              }
            }
          },
          "extensionData": {
            "id": "extension-payload-scope",
            "type": "Elsa.CompensationScope",
            "version": 1,
            "draftSchemaVersion": 1,
            "steps": [],
            "futurePayload": {
              "scope": {
                "type": "Elsa.CompensationScope",
                "version": 1,
                "draftSchemaVersion": 1,
                "steps": []
              }
            }
          }
        }
        """)!.AsObject();

    public static IActivityRegistry CreateActivityRegistry(params ActivityDescriptor[] extraDescriptors) =>
        new TestActivityRegistry(
        [
            new ActivityDescriptor
            {
                TypeName = "Elsa.SetVariable",
                Version = 1,
                Inputs = [new() { Name = "variable" }, new() { Name = "value" }, new() { Name = "payload" }]
            },
            new ActivityDescriptor { TypeName = "Elsa.If", Version = 1 },
            new ActivityDescriptor { TypeName = "Elsa.Sequence", Version = 1 },
            new ActivityDescriptor { TypeName = "Elsa.Flowchart", Version = 1 },
            new ActivityDescriptor { TypeName = "Elsa.StateMachine", Version = 1 },
            new ActivityDescriptor { TypeName = "Elsa.WriteLine", Version = 1, Inputs = [new() { Name = "text" }] },
            ..extraDescriptors
        ]);

    private sealed class TestActivityRegistry(IReadOnlyCollection<ActivityDescriptor> descriptors) : IActivityRegistry
    {
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<ActivityDescriptor> List() => descriptors;
        public ActivityDescriptor? Find(string activityType, int? version = default) =>
            version is { } exactVersion ? descriptors.FirstOrDefault(x => x.TypeName == activityType && x.Version == exactVersion) : null;
        public IEnumerable<ActivityDescriptor> FindAll(string activityType) => descriptors.Where(x => x.TypeName == activityType);
        public void MarkStale() { }
    }
}
