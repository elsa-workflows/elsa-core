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
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
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
            consumeContext ?? (IPublishEndpoint)bus;

        /// <summary>
        /// Gets the send endpoint provider to use.
        /// </summary>
        /// <remarks>
        /// Will use the current scopes consume context if one exists to maintain
        /// the conversation and correlation id.
        /// </remarks>
        protected ISendEndpointProvider SendEndpointProvider =>
            consumeContext ?? (ISendEndpointProvider)bus;

    }
}