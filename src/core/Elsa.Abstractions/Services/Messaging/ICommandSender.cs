﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;

namespace Elsa.Services
{
    /// <summary>
    /// Sends a command via AMQP (Rebus).
    /// </summary>
    public interface ICommandSender
    {
        Task SendAsync(object message, string? queueName = default, IDictionary<string, string>? headers = default, CancellationToken cancellationToken = default);
        Task DeferAsync(object message, Duration delay, string? queueName = default, IDictionary<string, string>? headers = default, CancellationToken cancellationToken = default);
    }

    public static class CommandSenderExtensions
    {
        public static Task SendAsync(this ICommandSender commandSender, object message, string? queueName = default, CancellationToken cancellationToken = default) => commandSender.SendAsync(message, queueName, default, cancellationToken);
    }
}