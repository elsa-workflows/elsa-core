using NodaTime;

namespace Elsa.Activities.Temporal.Common.Services
{
    /// <summary></summary>
    /// <remarks>The providers can support different formats. Quartz, for example, supports years.</remarks>
    public interface ICrontabParser
    {
        /// <summary>
        /// Converts a provider dependent cron string to <see cref="Instant"/>
        /// </summary>
        /// <param name="cronExpression"></param>
        /// <returns></returns>
        Instant GetNextOccurrence(string cronExpression);
    }
}
