using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Tests.Support;

internal static class NativeCompensationDraftTestData
{
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
          }
        }
        """)!.AsObject();
}
