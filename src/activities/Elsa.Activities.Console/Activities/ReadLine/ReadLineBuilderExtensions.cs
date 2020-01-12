using Elsa.Activities.Console;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ReadLineBuilderExtensions
    {
        public static ActivityBuilder ReadLine(this IBuilder builder) => builder.Then<ReadLine>();
    }
}