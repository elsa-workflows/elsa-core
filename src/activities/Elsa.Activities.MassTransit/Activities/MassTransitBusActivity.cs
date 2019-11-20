using System;
using Elsa.Services;
using MassTransit;

namespace Elsa.Activities.MassTransit.Activities
{
    public abstract class MassTransitBusActivity : Activity
    {
        private readonly ConsumeContext consumeContext;
        private readonly IBus bus;

        protected MassTransitBusActivity(IBus bus, ConsumeContext consumeContext)
        {
            if (bus == null)
            {
                throw new ArgumentNullException(nameof(bus));
            }

            this.bus = bus;
            this.consumeContext = consumeContext;
        }

        /// <summary>
        /// Gets the publish endpoint to use.
        /// </summary>
        /// <remarks>
        /// Will use the current scopes consume context if one exists to maintain
        /// the conversation and correlation id.
        /// </remarks>
        protected IPublishEndpoint PublishEndpoint =>
            consumeContext != null
                ? (IPublishEndpoint)consumeContext
                : (IPublishEndpoint)bus;

        /// <summary>
        /// Gets the send endpoint provider to use.
        /// </summary>
        /// <remarks>
        /// Will use the current scopes consume context if one exists to maintain
        /// the conversation and correlation id.
        /// </remarks>
        protected ISendEndpointProvider SendEndpointProvider =>
            consumeContext != null
                ? (ISendEndpointProvider)consumeContext
                : (ISendEndpointProvider)bus;

    }
}