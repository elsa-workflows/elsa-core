using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class JoinBuilderExtensions
    {
        public static ActivityBuilder Join(this IBuilder builder, Join.JoinMode mode) => builder.Then<Join>(x => x.WithMode(mode));
    }
}