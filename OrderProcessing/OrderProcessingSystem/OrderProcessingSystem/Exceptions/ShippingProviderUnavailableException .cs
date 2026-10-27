using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Exceptions
{
    public class ShippingProviderUnavailableException : Exception
    {
        public ShippingProviderUnavailableException(string message)
            : base(message)
        {
        }
    }
}

