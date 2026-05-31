using Elsa.Extensions;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Common.Serialization;

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
        management.Module.Services.Configure<SerializationTypeOptions>(options => options.RegisterTypeAlias(typeof(T), alias));
        return management;
    }
}
