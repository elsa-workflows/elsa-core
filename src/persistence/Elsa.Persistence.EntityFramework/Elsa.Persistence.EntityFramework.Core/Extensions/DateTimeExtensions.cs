using System;

namespace Elsa.Persistence.EntityFramework.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime WithKind(this DateTime dateTime, DateTimeKind kind) => new(dateTime.Ticks, kind);
    }
}