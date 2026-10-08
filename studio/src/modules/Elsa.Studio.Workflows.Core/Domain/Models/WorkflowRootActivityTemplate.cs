using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Describes a selectable root activity template for new workflow definitions.
/// </summary>
public record WorkflowRootActivityTemplate(
    string Key,
    string DisplayName,
    string Description,
    string Icon,
    string Color,
    Func<IIdentityGenerator, JsonObject> CreateRoot);
