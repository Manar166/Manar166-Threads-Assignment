using OrderProcessingSystem.Exceptions;
using OrderProcessingSystem.Interfaces;
using OrderProcessingSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Runners.Phase_4
{


    public class CairoExpress : IShippingCarrier
    {
        public string Name => "CairoExpress";

        private readonly Func<Order, CancellationToken, Task<decimal>> calculateShipping;
        private readonly HashSet<int> failForOrderIds;

        public CairoExpress(
            Func<Order, CancellationToken, Task<decimal>> calculateShipping,
            IEnumerable<int>? failForOrderIds = null)
        {
            this.calculateShipping = calculateShipping;
            this.failForOrderIds = new HashSet<int>(
                failForOrderIds ?? []);
        }

        public async Task<decimal> GetQuoteAsync(
            Order order,
            CancellationToken ct)
        {
            try
            {
                // Handshake
                await Task.Delay(150, ct);

                // Injected failure
                if (failForOrderIds.Contains(order.Id))
                {
                    throw new ShippingProviderUnavailableException(
                        "CairoExpress unavailable");
                }

                // Random carrier response time
                await Task.Delay(
                    Random.Shared.Next(300, 2001), ct);

                ct.ThrowIfCancellationRequested();

                // Built-in failure for even order IDs
                if (order.Id % 2 == 0)
                {
                    throw new ShippingProviderUnavailableException(
                        "CairoExpress unavailable");
                }

                decimal basePrice =await calculateShipping(order,ct);

                return basePrice * 0.95m;
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                Console.WriteLine("[CairoExpress] cancelled");
                throw;
            }
        }
    }


}
