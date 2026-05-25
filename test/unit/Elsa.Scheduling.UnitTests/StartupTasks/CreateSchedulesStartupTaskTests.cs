using Elsa.Common;
using Elsa.Scheduling.StartupTasks;
using Elsa.Workflows.Runtime.Tasks;

namespace Elsa.Scheduling.UnitTests.StartupTasks;

public class CreateSchedulesStartupTaskTests
{
    [Fact]
    public void Task_DependsOnPopulateRegistriesStartupTask()
    {
        var dependency = Assert.Single(typeof(CreateSchedulesStartupTask).GetCustomAttributes(typeof(TaskDependencyAttribute), false).Cast<TaskDependencyAttribute>());

        Assert.Equal(typeof(PopulateRegistriesStartupTask), dependency.DependencyTaskType);
    }
}
