using Elsa.Workflows.Core.Activities.Flowchart.Models;

namespace Elsa.Workflows.Core.Activities.Flowchart.Services;

public interface IActivityNodeDescriber
{
    ActivityNodeDescriptor DescribeActivity(Type activityType);
}