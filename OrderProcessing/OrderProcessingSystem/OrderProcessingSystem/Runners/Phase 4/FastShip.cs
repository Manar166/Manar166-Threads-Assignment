using OrderProcessingSystem.Exceptions;
using OrderProcessingSystem.Interfaces;
using OrderProcessingSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Runners.Phase_4
{


    public class FastShip : IShippingCarrier
    {
        public string Name => "FastShip";

        private readonly Func<Order, CancellationToken, Task<decimal>> calculateShipping;
        private readonly HashSet<int> failForOrderIds;

        public FastShip(
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
                        "FastShip unavailable");
                }

                // Random carrier response time
                await Task.Delay(
                    Random.Shared.Next(500, 1501), ct);

                ct.ThrowIfCancellationRequested();

                decimal basePrice = await calculateShipping(order,ct);

                return basePrice * 1.00m;
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                Console.WriteLine("[FastShip] cancelled");
                throw;
            }
        }
    }


}
