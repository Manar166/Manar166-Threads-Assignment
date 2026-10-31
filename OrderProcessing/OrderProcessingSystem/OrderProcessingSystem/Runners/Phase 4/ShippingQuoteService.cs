
using OrderProcessingSystem.Exceptions;
using OrderProcessingSystem.Interfaces;
using OrderProcessingSystem.Models;
using OrderProcessingSystem.Runners.Phase_4;
using System.Diagnostics;

public class ShippingQuoteService
{
    private readonly IShippingCarrier[] carriers;

    public ShippingQuoteService(IShippingCarrier[] carriers)
    {
        if (carriers.Length != 3)
            throw new ArgumentException(
                "Exactly three carriers are required.");

        this.carriers = carriers;
    }

    public async Task<ShippingQuote> GetFastestQuoteAsync(
        Order order,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var sw = Stopwatch.StartNew();

        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(ct);

        // Start all three carriers immediately.
        var pending = carriers
            .Select(carrier => (
                Carrier: carrier,
                Task: carrier.GetQuoteAsync(
                    order, linkedCts.Token)))
            .ToList();

        // Keep every task for final cleanup.
        Task[] allCarrierTasks = pending
            .Select(x => (Task)x.Task)
            .ToArray();

        // Overall time budget: 1.2 seconds.
        Task timeoutTask = Task.Delay(1200);

        // Completes when the caller cancels.
        Task cancellationTask = Task.Delay(
            Timeout.InfiniteTimeSpan,
            linkedCts.Token);

        try
        {
            while (pending.Count > 0)
            {
                // Wait for a carrier, timeout, or cancellation.
                Task completed = await Task.WhenAny(
                    pending.Select(x => (Task)x.Task)
                           .Append(timeoutTask)
                           .Append(cancellationTask));

                // Caller cancellation always takes priority.
                ct.ThrowIfCancellationRequested();

                // Deadline reached without a chosen quote.
                if (completed == timeoutTask ||
                    sw.ElapsedMilliseconds >= 1200)
                {
                    linkedCts.Cancel();

                    return new ShippingQuote(
                        "Flat rate",
                        25.00m,
                        sw.ElapsedMilliseconds);
                }

                // Find the carrier that completed.
                int index = pending.FindIndex(
                    x => x.Task == completed);

                var item = pending[index];
                pending.RemoveAt(index);

                try
                {
                    decimal price = await item.Task;

                    ct.ThrowIfCancellationRequested();

                    // First successful carrier wins.
                    linkedCts.Cancel();

                    return new ShippingQuote(
                        item.Carrier.Name,
                        price,
                        sw.ElapsedMilliseconds);
                }
                catch (ShippingProviderUnavailableException)
                {
                    Console.WriteLine(
                        $"[{item.Carrier.Name}] failed");

                    // Try the next carrier.
                }
            }

            // All three failed before the deadline.
            throw new ShippingProviderUnavailableException(
                "All carriers failed");
        }
        finally
        {
            // Cancel any unfinished carrier.
            linkedCts.Cancel();

            // Observe all carrier tasks, including failures.
            try
            {
                await Task.WhenAll(allCarrierTasks);
            }
            catch
            {
                // Failures were handled by the selection logic.
                // Remaining cancellations are expected.
            }

            sw.Stop();
        }
    }
}
