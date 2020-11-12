using System;
using System.Collections.Generic;

namespace Sample27.Models
{
    public class Cart
    {
        public Guid Id { get; set; }

        public string Username { get; set; }

        public IList<CartItem> Items { get; set; }
    }
}
