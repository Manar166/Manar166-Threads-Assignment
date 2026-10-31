using OrderProcessingSystem.Exceptions;
using OrderProcessingSystem.Interfaces;
using OrderProcessingSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Runners.Phase_4
{

    public class ShippingCarrier : IShippingCarrier
    {
        public string Name { get; }

        private readonly int minDelay;
        private readonly int maxDelay;
        private readonly decimal priceFactor;
        private readonly HashSet<int> failForOrderIds;
        private readonly bool failEvenOrderIds;

        public ShippingCarrier(
            string name,
            int minDelay,
            int maxDelay,
            decimal priceFactor,
            IEnumerable<int>? failForOrderIds = null,
            bool failEvenOrderIds = false)
        {
            Name = name;
            this.minDelay = minDelay;
            this.maxDelay = maxDelay;
            this.priceFactor = priceFactor;
            this.failForOrderIds =
                new HashSet<int>(failForOrderIds ?? []);
            this.failEvenOrderIds = failEvenOrderIds;
        }

        public async Task<decimal> GetQuoteAsync(
            Order order,
            CancellationToken ct)
        {
            try
            {
                // Handshake
                await Task.Delay(150, ct);

                // Simulated carrier response
                int delay = Random.Shared.Next(
                    minDelay, maxDelay + 1);

                await Task.Delay(delay, ct);

                ct.ThrowIfCancellationRequested();

                if (failForOrderIds.Contains(order.Id) ||
                    (failEvenOrderIds && order.Id % 2 == 0))
                {
                    throw new ShippingProviderUnavailableException(
                        $"{Name} unavailable");
                }

                // Replace with the exact shipping formula from 8.1
                decimal basePrice = 25m;

                return basePrice * priceFactor;
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                Console.WriteLine($"[{Name}] cancelled");
                throw;
            }
        }
    }

}
