using Elsa.Testing.Framework.Abstractions;
using Elsa.Workflows.Models;

namespace Elsa.Testing.Framework.Models;

public class Scenario
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public WorkflowDefinitionHandle WorkflowDefinitionHandle { get; set; } = null!;
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Variables { get; set; }
    public ICollection<Assertion> Assertions { get; set; } = [];
}