using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Runners.Phase_4
{
   public record ShippingQuote(
    string Carrier,
    decimal Price,
    long ElapsedMs);
}
