using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Exceptions
{
    public class InvalidCouponException:Exception
    {
        public InvalidCouponException(string coupon ,string message):base($"this coupon {coupon} is{message}")
        {
                
        }
    }
}
