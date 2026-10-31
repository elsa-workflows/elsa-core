using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Tests.Support;

/// <summary>An imported process as its root <c>Elsa.BpmnProcess</c> activity holds it: bound work, an unbound gateway and a nested scope.</summary>
internal static class BpmnProcessFixtures
{
    public const string RootId = "order-process";
    public const string NotifyActivityId = "order-process:node-Notify";
    public const string SubprocessId = "order-process:node-Shipping";
    public const string ShipActivityId = "order-process:node-Ship";

    public static JsonObject OrderProcess() => new()
    {
        ["id"] = RootId,
        ["type"] = "Elsa.BpmnProcess",
        ["workBindings"] = new JsonObject { ["node-Notify"] = NotifyActivityId },
        ["process"] = new JsonObject
        {
            ["elements"] = new JsonArray(
                new JsonObject { ["elementId"] = "NotifyWarehouse", ["elementType"] = "serviceTask", ["bindingRef"] = "node-Notify" },
                new JsonObject { ["elementId"] = "Gateway_1", ["elementType"] = "exclusiveGateway" },
                new JsonObject { ["elementId"] = "Shipping", ["elementType"] = "subProcess" })
        },
        ["activities"] = new JsonArray(
            new JsonObject { ["id"] = NotifyActivityId, ["type"] = "Elsa.WriteLine" },
            new JsonObject
            {
                ["id"] = SubprocessId,
                ["type"] = "Elsa.BpmnProcess",
                ["workBindings"] = new JsonObject { ["node-Ship"] = ShipActivityId },
                ["process"] = new JsonObject
                {
                    ["elements"] = new JsonArray(
                        new JsonObject { ["elementId"] = "Ship", ["elementType"] = "task", ["bindingRef"] = "node-Ship" },
                        new JsonObject { ["elementId"] = "Shipped", ["elementType"] = "endEvent" })
                },
                ["activities"] = new JsonArray(new JsonObject { ["id"] = ShipActivityId, ["type"] = "Elsa.WriteLine" })
            })
    };
}
