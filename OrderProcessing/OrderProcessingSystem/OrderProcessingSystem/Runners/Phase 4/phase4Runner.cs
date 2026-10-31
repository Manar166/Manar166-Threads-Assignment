
using OrderProcessingSystem;
using OrderProcessingSystem.Exceptions;
using OrderProcessingSystem.Models;
using OrderProcessingSystem.Phase_4;
using OrderProcessingSystem.Repos;
using OrderProcessingSystem.Runners.Phase_4;
using OrderProcessingSystem.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace OrderProcessingSystem.Runners
{
    public class Phase4Runner
    {
        private readonly DataSeeder _dataSeeder;
        private readonly OrderProcessor _orderProcessor;
        private readonly OrderRepository _orderRepository;
        private readonly JsonFileStore _store;
        private readonly CustomerDashboardService dashboardService;
        private readonly ShippingQuoteService quoteService;
        private readonly ShippingQuoteService failingQuoteService;

        public Phase4Runner(
            DataSeeder dataSeeder,
            OrderProcessor orderProcessor,
            OrderRepository orderRepository,
            JsonFileStore store,
            CustomerDashboardService dashboardService,
            ShippingQuoteService quoteService,
            ShippingQuoteService failingQuoteService)
        {
            _dataSeeder = dataSeeder;
            _orderProcessor = orderProcessor;
            _orderRepository = orderRepository;
            _store = store;
            this.dashboardService = dashboardService;
            this.quoteService = quoteService;
            this.failingQuoteService = failingQuoteService;
        }

        private record OrderProgress(
            int OrderId,
            string Status,
            string? Reason,
            double ElapsedSeconds,
            Exception? Error);

        public async Task RunAsync( CancellationToken ct)
        {
            // R1 - Reset and load all 20 orders
            await _dataSeeder.ResetAsync(ct);

            List<Order> orders = await _store.ReadAsync<Order>(
                _store.GetPath(DataFiles.IncomingOrders), ct);

            if (orders.Count != 20)
                throw new InvalidOperationException(
                    $"Expected 20 orders, found {orders.Count}.");

            // Channel for completed order results
            var channel =
                Channel.CreateUnbounded<OrderProgress>();

            // Each order publishes its result when finished
            async Task ProcessAndPublishAsync(Order order)
            {
                var sw = Stopwatch.StartNew();
                Exception? error = null;

                try
                {
                    await _orderProcessor.ProcessOrderAsync(order, ct);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    sw.Stop();

                    channel.Writer.TryWrite(
                        new OrderProgress(
                            order.Id,
                            error == null
                                ? order.Status.ToString()
                                : "Error",
                            order.FailureReason,
                            sw.Elapsed.TotalSeconds,
                            error));
                }
            }

            // Start all orders before awaiting any completion
            List<Task> tasks = new();

            foreach (Order order in orders)
            {
                tasks.Add(ProcessAndPublishAsync(order));
            }

            // Close channel only after all producers finish
            async Task CompleteChannelAsync()
            {
                try
                {
                    await Task.WhenAll(tasks);
                }
                finally
                {
                    channel.Writer.TryComplete();
                }
            }

            Task completionTask = CompleteChannelAsync();

            // R2 + R3 + R4 - Read results in arrival order
            int counter = 0;
            int unexpectedErrors = 0;

            await foreach (OrderProgress result in
                channel.Reader.ReadAllAsync())
            {
                counter++;

                string reason = string.IsNullOrWhiteSpace(result.Reason)
                    ? ""
                    : $" ({result.Reason.Trim()})";

                if (result.Error != null)
                {
                    unexpectedErrors++;
                    reason = $" ({result.Error.Message})";
                }

                Console.WriteLine(
                    $"[{counter,2}/20] " +
                    $"Order #{result.OrderId} -> " +
                    $"{result.Status}{reason} " +
                    $"after {result.ElapsedSeconds:F1} s");
            }

            await completionTask;

            // R5 - Final counts from persisted orders
            List<Order> savedOrders =
                await _orderRepository.GetAllAsync(
                    CancellationToken.None);

            Console.WriteLine();
            Console.WriteLine("===== FINAL STATUS COUNTS =====");

            foreach (var group in savedOrders
                .GroupBy(o => o.Status)
                .OrderBy(g => g.Key))
            {
                Console.WriteLine(
                    $"{group.Key,-16}: {group.Count()}");
            }

            Console.WriteLine($"Total saved: {savedOrders.Count}");
            Console.WriteLine($"Progress lines: {counter}");

            if (savedOrders.Count != 20 || unexpectedErrors > 0)
            {
                throw new InvalidOperationException(
                    $"Run incomplete: saved={savedOrders.Count}, " +
                    $"unexpected errors={unexpectedErrors}.");
            }




            //--------------------

            foreach (int customerId in new[] { 3, 99 })
            {
                var sw = Stopwatch.StartNew();

                try
                {
                    CustomerDashboard dashboard =
                        await dashboardService.LoadAsync(customerId, ct);

                    sw.Stop();

                    Console.WriteLine(
                        $"Customer #{customerId}: {dashboard.Customer.Name}");

                    Console.WriteLine($"Orders: {dashboard.Orders.Count}");
                    Console.WriteLine($"Payments: {dashboard.Payments.Count}");
                    Console.WriteLine($"Emails: {dashboard.Emails.Count}");
                }
                catch (AggregateException ex)
                {
                    sw.Stop();

                    foreach (Exception error in ex.InnerExceptions)
                    {
                        Console.WriteLine($"Error: {error.Message}");
                    }
                }

                Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
            }

            Console.WriteLine("=== Fastest Shipping Quotes ===");

            foreach (Order order in orders
                .Where(o => o.Id >= 1 && o.Id <= 5)
                .OrderBy(o => o.Id))
            {
                ShippingQuote quote =
                    await quoteService.GetFastestQuoteAsync(order, ct);

                Console.WriteLine(
                    $"Order #{order.Id} -> " +
                    $"{quote.Carrier}, " +
                    $"Price: {quote.Price:F2}, " +
                    $"Time: {quote.ElapsedMs} ms");
            }

            Console.WriteLine();
            Console.WriteLine("=== All Carriers Failure Test ===");

            Order order6 = orders.Single(o => o.Id == 6);

            try
            {
                await failingQuoteService.GetFastestQuoteAsync(
                    order6, ct);
            }
            catch (ShippingProviderUnavailableException ex)
            {
                Console.WriteLine($"Order #6 -> {ex.Message}");

            }

        }



    }
}
