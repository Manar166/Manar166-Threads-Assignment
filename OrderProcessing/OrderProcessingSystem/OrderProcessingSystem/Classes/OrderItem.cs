using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Classes
{
    public class OrderItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public OrderItem() { }
    }
}
