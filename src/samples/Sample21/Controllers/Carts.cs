using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Sample21.Messages;
using Sample21.Models;

namespace Sample21.Controllers
{
    public class Carts : ICarts
    {
        private static readonly ConcurrentBag<Cart> ActiveCarts = new ConcurrentBag<Cart>();

        private readonly ISendEndpointProvider sender;

        public Carts(ISendEndpointProvider sender)
        {
            this.sender = sender;
        }

        public Task<Cart> Cart(string username)
        {
            return Task.FromResult(ActiveCarts.FirstOrDefault(x => x.Username == username));
        }

        public async Task AddItem(string username, CartItem item)
        {
            var cart = await Cart(username);

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    Items = new List<CartItem>()
                };

                ActiveCarts.Add(cart);

                await sender.Send(new CartCreated
                {
                    CartId = cart.Id,
                    Timestamp = DateTime.UtcNow
                });
            }

            var exitingItem = cart.Items.FirstOrDefault(x => x.ProductSku == item.ProductSku);
            if (exitingItem != null)
            {
                exitingItem.Quantity += item.Quantity;
            }
            else
            {
                cart.Items.Add(item);
            }

            await sender.Send(new CartItemAdded
            {
                CartId = cart.Id,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}