using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities.Flowchart.Models;

public record TransposeContext(Connection Connection, ActivityNodeDescriptor SourceDescriptor, ActivityNodeDescriptor TargetDescriptor);