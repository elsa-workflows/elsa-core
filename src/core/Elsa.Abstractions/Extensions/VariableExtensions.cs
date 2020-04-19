

// ReSharper disable once CheckNamespace
namespace Elsa.Models
{
    public static class VariableExtensions
    {
        public static T GetValue<T>(this Variable? variable) => (T)variable?.Value!;
    }
}