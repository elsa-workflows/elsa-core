using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Elsa.Persistence.DocumentDb.Helpers
{
    internal static class TimeHelper
    {
        private static readonly DateTime epochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        internal static int ToEpoch(this DateTime date)
        {
            if (date.Equals(DateTime.MinValue)) return int.MinValue;
            TimeSpan epochTimeSpan = date - epochDateTime;
            return (int)epochTimeSpan.TotalSeconds;
        }

        internal static DateTime ToDateTime(this int totalSeconds) => epochDateTime.AddSeconds(totalSeconds);

        internal static string TryParseToEpoch(this string s)
        {
            return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime date)
                ? date.ToEpoch().ToString(CultureInfo.InvariantCulture)
                : s;
        }
    }
}
