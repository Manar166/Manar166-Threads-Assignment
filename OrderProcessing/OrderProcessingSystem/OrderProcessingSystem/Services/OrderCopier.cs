using OrderProcessingSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Services
{
    public static class OrderCopier
    {
        public static Order CreateOrderCopy(Order source)
        {
            return new Order
            {
                Id = source.Id,
                CustomerId = source.CustomerId,
                CouponCode = source.CouponCode,
                Items = source.Items.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList()
            };
        }
    }
}
