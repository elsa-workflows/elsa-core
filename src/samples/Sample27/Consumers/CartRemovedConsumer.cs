using System.Threading.Tasks;
using MassTransit;
using Sample27.Controllers;
using Sample27.Messages;

namespace Sample27.Consumers
{
    public class CartRemovedConsumer : IConsumer<CartRemovedEvent>
    {
        private readonly ICarts carts;

        public CartRemovedConsumer(ICarts carts)
        {
            this.carts = carts;
        }

        public Task Consume(ConsumeContext<CartRemovedEvent> context)
        {
            carts.Remove(context.Message.CartId);

            return Task.CompletedTask;
        }
    }
}
