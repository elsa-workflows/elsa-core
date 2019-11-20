using System;
using System.Threading.Tasks;
using Sample21.Models;

namespace Sample21.Controllers
{
    public interface ICarts
    {
        Task<Cart> Cart(string username);
        Task AddItem(string username, CartItem item);
        Task Submit(string username);
        void Remove(Guid cartId);
    }
}