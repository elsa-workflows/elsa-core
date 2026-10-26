using Elsa.Common.Serialization;
using Elsa.Extensions;
using Elsa.UserTasks.Models;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UserTask = Elsa.UserTasks.Activities.UserTask;
using UserTasksShellFeature = Elsa.UserTasks.ShellFeatures.UserTasksFeature;

namespace Elsa.UserTasks.UnitTests;

/// <summary>
/// Enabling the User Tasks module makes its <see cref="UserTask"/> activity available to workflows, so a host never
/// has to register the activity by hand. Regression for elsa-workflows/elsa-core#8915.
/// </summary>
public sealed class UserTasksFeatureActivityRegistrationTests
{
    private const string UserTaskTypeName = "Elsa.UserTask";

    [Fact(DisplayName = "UseUserTasks registers the UserTask activity")]
    public async Task UseUserTasks_RegistersTheUserTaskActivity()
    {
        var services = new ServiceCollection();
        services.AddElsa(elsa => elsa.UseUserTasks());
        await using var serviceProvider = services.BuildServiceProvider();

        await serviceProvider.GetRequiredService<IActivityRegistryPopulator>().PopulateRegistryAsync();

        var descriptor = serviceProvider.GetRequiredService<IActivityRegistry>().Find(UserTaskTypeName);
        Assert.Equal(typeof(UserTask), descriptor?.ClrType);
    }

    [Fact(DisplayName = "The User Tasks shell feature registers the UserTask activity")]
    public void UserTasksShellFeature_RegistersTheUserTaskActivity()
    {
        var services = new ServiceCollection();
        new UserTasksShellFeature().ConfigureServices(services);
        using var serviceProvider = services.BuildServiceProvider();

        var activityTypes = serviceProvider.GetRequiredService<IOptions<ManagementOptions>>().Value.ActivityTypes;
        Assert.Contains(typeof(UserTask), activityTypes);
    }

    [Fact(DisplayName = "UseUserTasks registers a UserTaskMaterialization type alias")]
    public void UseUserTasks_RegistersUserTaskMaterializationTypeAlias()
    {
        var services = new ServiceCollection();
        services.AddElsa(elsa => elsa.UseUserTasks());
        using var serviceProvider = services.BuildServiceProvider();

        var aliases = serviceProvider.GetRequiredService<IOptions<SerializationTypeOptions>>().Value;
        Assert.True(aliases.TypeAliasDictionary.ContainsKey(typeof(UserTaskMaterialization)));
        Assert.Equal(nameof(UserTaskMaterialization), aliases.TypeAliasDictionary[typeof(UserTaskMaterialization)]);
    }

    [Fact(DisplayName = "The User Tasks shell feature registers a UserTaskMaterialization type alias")]
    public void UserTasksShellFeature_RegistersUserTaskMaterializationTypeAlias()
    {
        var services = new ServiceCollection();
        new UserTasksShellFeature().ConfigureServices(services);
        using var serviceProvider = services.BuildServiceProvider();

        var aliases = serviceProvider.GetRequiredService<IOptions<SerializationTypeOptions>>().Value;
        Assert.True(aliases.TypeAliasDictionary.ContainsKey(typeof(UserTaskMaterialization)));
        Assert.Equal(nameof(UserTaskMaterialization), aliases.TypeAliasDictionary[typeof(UserTaskMaterialization)]);
    }
}
