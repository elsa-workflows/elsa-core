using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;

namespace Elsa.Workflows.Runtime.Features;

/// <summary>
/// Installs the default runtime services.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
public class DefaultWorkflowRuntimeFeature(IModule module) : FeatureBase(module);