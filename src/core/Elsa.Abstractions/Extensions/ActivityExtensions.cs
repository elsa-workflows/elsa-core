using Elsa.Models;
using Elsa.Services;

namespace Elsa.Extensions
{
    public static class ActivityExtensions
    {
        public static ActivityInstance ToActivityInstance(this IActivity activity) =>
            new ActivityInstance
            {
                Id = activity.Id,
                Type = activity.Type,
                Output = activity.Output,
                State = activity.State
            };
    }
}