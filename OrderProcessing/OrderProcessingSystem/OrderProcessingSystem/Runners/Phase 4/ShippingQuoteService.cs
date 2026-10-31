using OrderProcessingSystem.Exceptions;
using OrderProcessingSystem.Interfaces;
using OrderProcessingSystem.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessingSystem.Runners.Phase_4
{
    public class ShippingQuoteService
    {
        private readonly IShippingCarrier[] _carriers;

        public ShippingQuoteService(IShippingCarrier[] carriers)
        {
            _carriers = carriers;
        }


        public async Task<ShippingQuote> GetFastestQuoteAsync(
            Order order,
            CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            using var linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(ct);

            using var timeoutCts = new CancellationTokenSource();

            CancellationToken carrierToken = linkedCts.Token;

            // Start all carriers concurrently.
            var pending = _carriers
                .Select(carrier => (
                    Carrier: carrier,
                    Task: carrier.GetQuoteAsync(order, carrierToken)))
                .ToList();

            // Independent deadline for the whole operation.
            Task timeoutTask = Task.Delay(1200, timeoutCts.Token);

            try
            {
                while (pending.Count > 0)
                {
                    // Wait for carrier completion or timeout.
                    Task completed = await Task.WhenAny(
                        pending.Select(x => (Task)x.Task)
                            .Append(timeoutTask));

                    ct.ThrowIfCancellationRequested();

                    if (completed == timeoutTask)
                    {
                        linkedCts.Cancel();

                        return new ShippingQuote(
                            "Flat rate", 25.00m, sw.ElapsedMilliseconds);
                    }

                    int index = pending.FindIndex(
                        x => x.Task == completed);

                    var item = pending[index];
                    pending.RemoveAt(index);

                    try
                    {
                        decimal price = await item.Task;

                        // A successful carrier wins.
                        linkedCts.Cancel();

                        return new ShippingQuote(
                            item.Carrier.Name,
                            price,
                            sw.ElapsedMilliseconds);
                    }
                    catch (ShippingProviderUnavailableException)
                    {
                        // This carrier failed; try the next one.
                    }
                }

                throw new ShippingProviderUnavailableException(
                    "All carriers failed");
            }
            finally
            {
                linkedCts.Cancel();
                timeoutCts.Cancel();
                sw.Stop();

                // Observe remaining carrier task exceptions.
                foreach (var item in pending)
                {
                    _ = item.Task.Exception;
                }
            }
        }

    }
}
