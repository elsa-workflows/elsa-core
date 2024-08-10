using Elsa.Extensions;
using Elsa.Workflows.Management.Features;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management;

[PublicAPI]
public static class WorkflowManagementFeatureExtensions
{
    public static WorkflowManagementFeature AddVariableTypeAndAlias<T>(this WorkflowManagementFeature management, string category)
    {
        return management.AddVariableTypeAndAlias<T>(typeof(T).Name, category);
    }
    
    public static WorkflowManagementFeature AddVariableTypeAndAlias<T>(this WorkflowManagementFeature management, string alias, string category)
    {
        management.AddVariableType<T>(category);
        management.Module.AddTypeAlias<T>(alias);
        return management;
    }
}