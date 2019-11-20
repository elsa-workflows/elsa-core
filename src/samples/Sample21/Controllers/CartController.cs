using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Esprima.Ast;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sample21.Models;

namespace Sample21.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private const string Username = "sampleUser";

        private readonly ICarts _carts;

        public CartController(ICarts carts)
        {
            _carts = carts;
        }

        // GET: api/Cart
        [HttpGet]
        public async Task<Cart> Get()
        {
            var cart = await _carts.Cart(Username);

            return cart ?? new Cart
            {
                Id = Guid.Empty,
                Username = Username,
                Items = new List<CartItem>()
            };
        }

        // POST: api/Cart
        [HttpPost]
        public async Task Post([FromBody] CartItem item)
        {
            await _carts.AddItem(Username, item);
        }

        // POST: api/cart/submit
        [HttpPost]
        [Route("submit")]
        public async Task SubmitOrder()
        {
            await _carts.Submit(Username);
        }
    }
}
