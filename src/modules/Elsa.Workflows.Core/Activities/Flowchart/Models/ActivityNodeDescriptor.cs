using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities.Flowchart.Models;

public record ActivityNodeDescriptor(Type ActivityRuntimeType, string ActivityTypeName, ICollection<Port> OutPorts);