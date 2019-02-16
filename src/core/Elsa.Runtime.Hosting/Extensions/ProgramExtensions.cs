using System.ComponentModel;
using Elsa.Runtime.Hosting.TypeConverters;
using NodaTime;

namespace Elsa.Runtime.Hosting.Extensions
{
    public static class ProgramExtensions
    {
        public static void ConfigureTypeConverters()
        {
            TypeDescriptor.AddAttributes(
                typeof(Period),
                new TypeConverterAttribute(typeof(PeriodTypeConverter)));
        }
    }
}