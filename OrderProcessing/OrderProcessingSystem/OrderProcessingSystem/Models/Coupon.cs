using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Models
{
    public class Coupon
    {
        public string? Code { get; set; }
        public int DiscountPercent { get; set; }
        public DateTime ExpiryDate { get; set; }

        public Coupon() { }
    }
}
