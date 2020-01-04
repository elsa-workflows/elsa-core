using Elsa.Services.Models;

namespace Elsa.Activities.Flowcharts
{
    public static class FlowchartExtensions
    {
        public static Flowchart StartWith(this Flowchart flowchart, IActivity activity)
        {
            flowchart.Activities.Add(activity);
            return flowchart;
        }
    }
}