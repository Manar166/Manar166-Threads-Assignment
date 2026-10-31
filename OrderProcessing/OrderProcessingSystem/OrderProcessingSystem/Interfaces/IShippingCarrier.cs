using OrderProcessingSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Interfaces
{
    public interface IShippingCarrier
    {
        string Name { get; }

        Task<decimal> GetQuoteAsync(Order order, CancellationToken ct);


    }
}
