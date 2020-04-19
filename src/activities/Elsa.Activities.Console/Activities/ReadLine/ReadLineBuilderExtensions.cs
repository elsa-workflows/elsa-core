using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Console
{
    public static class ReadLineBuilderExtensions
    {
        public static IActivityBuilder ReadLine(this IBuilder builder) => builder.Then<ReadLine>();
    }
}