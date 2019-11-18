using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sample21.Models
{
    public class Cart
    {
        public Guid Id { get; set; }

        public string Username { get; set; }

        public IList<CartItem> Items { get; set; }
    }

    public class CartItem
    {
        public int Quantity { get; set; }

        public string ProductSku { get; set; }
    }
}
