using System;
using System.Globalization;

namespace Elsa.Persistence.DocumentDb.Extensions
{
    internal static class DateTimeExtensions
    {
        private static readonly DateTime EpochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        internal static int ToEpoch(this DateTime date)
        {
            if (date.Equals(DateTime.MinValue)) return int.MinValue;
            var epochTimeSpan = date - EpochDateTime;
            return (int) epochTimeSpan.TotalSeconds;
        }

        internal static DateTime ToDateTime(this int totalSeconds) => EpochDateTime.AddSeconds(totalSeconds);

        internal static string TryParseToEpoch(this string s)
        {
            return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date) ? 
                date.ToEpoch().ToString(CultureInfo.InvariantCulture) : s;
        }
    }
}