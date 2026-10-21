using Elsa.Extensions;
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
}
