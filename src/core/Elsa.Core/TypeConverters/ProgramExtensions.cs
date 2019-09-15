using System.ComponentModel;
using NodaTime;

namespace Elsa.TypeConverters
{
    /// <summary>
    /// A helper class that registers NodaTime converters to enable parsing of appsettings.
    /// </summary>
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