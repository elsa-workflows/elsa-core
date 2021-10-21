using System;
using Elsa.Services;
using MassTransit;
using MassTransit.Context;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public abstract class MassTransitBusActivity : Activity
    {
        private readonly ConsumeContext _consumeContext;
        private readonly IBus _bus;

        protected MassTransitBusActivity(IBus bus, ConsumeContext consumeContext)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _consumeContext = consumeContext;
        }

        /// <summary>
        /// Gets the publish endpoint to use.
        /// </summary>
        /// <remarks>
        /// Will use the current scopes consume context if one exists to maintain
        /// the conversation and correlation id.
        /// </remarks>
        protected IPublishEndpoint PublishEndpoint =>
            _consumeContext != MissingConsumeContext.Instance ? _consumeContext : _bus;

        /// <summary>
        /// Gets the send endpoint provider to use.
        /// </summary>
        /// <remarks>
        /// Will use the current scopes consume context if one exists to maintain
        /// the conversation and correlation id.
        /// </remarks>
        protected ISendEndpointProvider SendEndpointProvider =>
            _consumeContext != MissingConsumeContext.Instance ? _consumeContext : _bus;
    }
}