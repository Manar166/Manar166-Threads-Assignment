using OrderProcessingSystem.Exceptions;
using OrderProcessingSystem.Interfaces;
using OrderProcessingSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Runners.Phase_4
{


    public class NileCourier : IShippingCarrier
    {
        public string Name => "NileCourier";

        private readonly Func<Order, CancellationToken, Task<decimal>> calculateShipping;
        private readonly HashSet<int> failForOrderIds;

        public NileCourier(
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

                // Injected failure: allows Order #6 test
                if (failForOrderIds.Contains(order.Id))
                {
                    throw new ShippingProviderUnavailableException(
                        "NileCourier unavailable");
                }

                // Random carrier response time
                await Task.Delay(
                    Random.Shared.Next(800, 1201), ct);

                ct.ThrowIfCancellationRequested();

                decimal basePrice =await calculateShipping(order,ct);

                return basePrice * 1.10m;
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                Console.WriteLine("[NileCourier] cancelled");
                throw;
            }
        }
    }


}
