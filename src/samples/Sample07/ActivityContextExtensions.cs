using Elsa.Services.Models;

namespace Sample07
{
    /// <summary>
    /// Extension classes help making workflow implementations more readable.
    /// </summary>
    public static class ActivityContextExtensions
    {
        public static int GetCounter(this ActivityExecutionContext context) => context.GetVariable<int>("Counter");
    }
}