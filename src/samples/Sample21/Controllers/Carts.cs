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
        private static readonly ConcurrentDictionary<string, Cart> ActiveCarts = new ConcurrentDictionary<string, Cart>();

        private readonly ISendEndpointProvider sender;

        public Carts(ISendEndpointProvider sender)
        {
            this.sender = sender;
        }

        public Task<Cart> Cart(string username)
        {
            ActiveCarts.TryGetValue(username, out var cart);

            return Task.FromResult(cart);
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

                if (ActiveCarts.TryAdd(username, cart))
                {
                    await sender.Send(new CartCreated
                    {
                        CartId = cart.Id,
                        UserName = cart.Username,
                        Timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    ActiveCarts.TryGetValue(username, out cart);
                }
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
                UserName = cart.Username,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task Submit(string username)
        {
            if (ActiveCarts.TryRemove(username, out var cart))
            {
                await sender.Send(new OrderSubmitted
                {
                    OrderId = Guid.NewGuid(),
                    CartId = cart.Id,
                    UserName = username,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        public void Remove(Guid cartId)
        {
            var cart = ActiveCarts.Values.FirstOrDefault(x => x.Id == cartId);
            if (cart != null)
            {
                ActiveCarts.TryRemove(cart.Username, out _);
            }
        }
    }
}