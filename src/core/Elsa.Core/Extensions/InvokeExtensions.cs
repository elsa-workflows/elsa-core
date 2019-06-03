using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Elsa.Core.Extensions
{
    public static class InvokeExtensions
    {
        /// <summary>
        /// Safely invoke methods by catching non fatal exceptions and logging them.
        /// </summary>
        public static void Invoke<T>(this IEnumerable<T> events, Action<T> dispatch, ILogger logger)
        {
            foreach (var sink in events)
            {
                try
                {
                    dispatch(sink);
                }
                catch (Exception ex)
                {
                    HandleException(ex, logger, typeof(T).Name, sink.GetType().FullName);
                }
            }
        }

        /// <summary>
        /// Safely invoke methods by catching non fatal exceptions and logging them.
        /// </summary>
        public static async Task InvokeAsync<T>(this IEnumerable<T> events, Func<T, Task> dispatch, ILogger logger)
        {
            foreach (var sink in events)
            {
                try
                {
                    await dispatch(sink);
                }
                catch (Exception ex)
                {
                    HandleException(ex, logger, typeof(T).Name, sink.GetType().FullName);
                }
            }
        }

        private static void HandleException(Exception ex, ILogger logger, string sourceType, string method)
        {
            if (ex.IsFatal())
                throw ex;

            logger.LogError(ex, "{Type} thrown from {Method} by {Exception}",
                sourceType,
                method,
                ex.GetType().Name);
        }
    }
}
