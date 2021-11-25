using System;
using System.Text;

namespace Elsa.Scripting.JavaScript.Extensions
{
    internal static class StringExtensions
    {
        public static string ToBase64(this string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        public static string FromBase64(this string value)
        {
            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}