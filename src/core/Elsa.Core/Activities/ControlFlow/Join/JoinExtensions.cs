using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class JoinExtensions
    {
        public static Join WithMode(this Join activity, Join.JoinMode value) => activity.With(x => x.Mode, value);
    }
}