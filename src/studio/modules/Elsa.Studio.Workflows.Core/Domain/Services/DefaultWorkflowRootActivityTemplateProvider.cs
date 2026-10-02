using System.Text.Json.Nodes;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using MudBlazor;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// Provides the built-in workflow root activity templates.
/// </summary>
public class DefaultWorkflowRootActivityTemplateProvider : IWorkflowRootActivityTemplateProvider
{
    /// <summary>
    /// The built-in Flowchart root activity type name.
    /// </summary>
    public const string FlowchartKey = "Elsa.Flowchart";

    /// <summary>
    /// The built-in Sequence root activity type name.
    /// </summary>
    public const string SequenceKey = "Elsa.Sequence";

    /// <summary>
    /// The built-in StateMachine root activity type name.
    /// </summary>
    public const string StateMachineKey = "Elsa.StateMachine";

    private readonly IReadOnlyCollection<WorkflowRootActivityTemplate> _templates =
    [
        new(
            FlowchartKey,
            "Flowchart",
            "Flexible graph-based workflows.",
            ElsaStudioIcons.Tabler.GitFork,
            "#06b6d4",
            identityGenerator => CreateRoot(identityGenerator, FlowchartKey, "Flowchart1")),
        new(
            SequenceKey,
            "Sequence",
            "Ordered step-by-step workflows.",
            Icons.Material.Outlined.FormatListNumbered,
            "#16a34a",
            identityGenerator =>
            {
                var root = CreateRoot(identityGenerator, SequenceKey, "Sequence1");
                root["activities"] = new JsonArray();
                return root;
            }),
        new(
            StateMachineKey,
            "State machine",
            "State and transition driven workflows.",
            Icons.Material.Outlined.AccountTree,
            "#7c3aed",
            identityGenerator =>
            {
                var root = CreateRoot(identityGenerator, StateMachineKey, "StateMachine1");
                root["states"] = new JsonArray();
                root["transitions"] = new JsonArray();
                return root;
            })
    ];

    /// <inheritdoc />
    public IReadOnlyCollection<WorkflowRootActivityTemplate> List() => _templates;

    /// <inheritdoc />
    public WorkflowRootActivityTemplate GetDefault() => _templates.First();

    /// <inheritdoc />
    public WorkflowRootActivityTemplate? Find(string? key)
    {
        return string.IsNullOrWhiteSpace(key) ? null : _templates.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    private static JsonObject CreateRoot(IIdentityGenerator identityGenerator, string type, string name) =>
        new()
        {
            ["id"] = identityGenerator.GenerateId(),
            ["type"] = type,
            ["version"] = 1,
            ["name"] = name
        };
}
