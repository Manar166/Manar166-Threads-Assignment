using OrderProcessingSystem.Classes;
using OrderProcessingSystem.Enums;
using OrderProcessingSystem.Generators;
using OrderProcessingSystem.NewFolder;
using OrderProcessingSystem.Repos;
using OrderProcessingSystem.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;

namespace OrderProcessingSystem.Runners
{
    public class Phase2Runner
    {
        private readonly IncomingOrderRepository _incomingorderRepo;
        private readonly OrderPricer _orderPricer;
        private readonly DataSeeder _dataSeeder;
        private readonly RaceConditionDemo _raceConditionDemo;
        private readonly OrderValidator _orderValidator;
        private readonly OrderProcessorV2 orderProcessorV2;
        //private ProductRepository productRepo;
        private OrderRepository _orderRepository;
        private ReportGenerator _reportGenerator;

        public Phase2Runner(IncomingOrderRepository incomingorderRepo, OrderPricer orderPricer, DataSeeder dataSeeder, RaceConditionDemo raceConditionDemo, OrderValidator orderValidator, OrderProcessorV2 orderProcessorV2, OrderRepository orderProcessor, ReportGenerator reportGenerator)
        {
            _incomingorderRepo = incomingorderRepo;
            _orderPricer = orderPricer;
            _dataSeeder = dataSeeder;
            _raceConditionDemo = raceConditionDemo;
            _orderValidator = orderValidator;
            this.orderProcessorV2 = orderProcessorV2;
            _orderRepository = orderProcessor;
            _reportGenerator = reportGenerator;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            await _dataSeeder.ResetAsync(ct);

            await _raceConditionDemo.RunAsync(ct);

            await _dataSeeder.ResetAsync(ct);


            var allOrders = await _incomingorderRepo.GetAllAsync(ct);
            var order1 = allOrders.Find(n => n.Id == 1);

            Order parallelOrder = OrderCopier.CreateOrderCopy(order1);
            Order sequentialOrder = OrderCopier.CreateOrderCopy(order1);
            var stopwatch = Stopwatch.StartNew();
            await _orderPricer.PriceOrderSequentialAsync(sequentialOrder, ct);

            stopwatch.Stop();
            Console.WriteLine($"Sequantial pricing: {stopwatch.ElapsedMilliseconds} ms");


            stopwatch.Restart();
            await _orderPricer.PriceOrderAsync(parallelOrder, ct);

            stopwatch.Stop();

            Console.WriteLine($"Parallel pricing: {stopwatch.ElapsedMilliseconds} ms");


            var Tasks = new List<Task>();
            foreach (var item in allOrders)
            {
                Tasks.Add(_orderValidator.ValidateOrderAsync(item, ct));

            }
            foreach (var task in Tasks)
            {
                await task;
            }

            var validOrders = allOrders.Where(o => o.Status == Enums.OrderStatus.Validated).ToList();

            stopwatch.Restart();

            List<Task<bool>> processingTasks = new();
            foreach (var validOrder in validOrders)
            {

                processingTasks.Add(orderProcessorV2.ProcessValidatedOrderAsync(validOrder, ct));

            }

            Console.WriteLine($"First task status: {processingTasks[0].Status}");
            await Task.Delay(1000,ct);

            for (int i = 0; i < processingTasks.Count; i++)
            {
                Console.WriteLine($"Order #{validOrders[i].Id}: " + $"{processingTasks[i].Status}");


            }

            for (int i = 0; i < processingTasks.Count; i++)
            {
                var task = processingTasks[i];
                var validOrder = validOrders[i];

                try
                {
                    bool result = await task;
                    Console.WriteLine($"Order #{validOrder.Id}: {task.Status}, Result = {result}");


                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {


                    validOrder.Status = OrderStatus.Pending;
                    validOrder.FailureReason = "Cancelled";

                    await _orderRepository.SaveAsync(
                        validOrder, CancellationToken.None);

                    Console.WriteLine($"Order #{validOrder.Id}: Cancelled");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Order #{validOrder.Id}: {task.Status}, Error = {ex.Message}");


                    validOrder.Status = OrderStatus.Failed;
                    validOrder.FailureReason = ex.Message;

                    await _orderRepository.SaveAsync(
                        validOrder, CancellationToken.None);
                }

            }

            stopwatch.Stop();

            // Step 6: Final task statuses
            Console.WriteLine("\n===== Final Task Statuses =====");

            for (int i = 0; i < processingTasks.Count; i++)
            {
                Console.WriteLine(
                    $"Order #{validOrders[i].Id}: " + $"{processingTasks[i].Status}");

            }

            Console.WriteLine(
                $"Concurrent processing time: " + $"{stopwatch.Elapsed.TotalSeconds:F2} seconds");


            // Step 7: Verify orders.json
            var savedOrders = await _orderRepository.GetAllAsync(
                CancellationToken.None);

            Console.WriteLine("\n===== Phase 2 Summary =====");

            Console.WriteLine($"Total Orders: {savedOrders.Count}");

            Console.WriteLine(
                $"Completed: {savedOrders.Count(o => o.Status == OrderStatus.Completed)}");

            Console.WriteLine(
                $"Rejected: {savedOrders.Count(o => o.Status == OrderStatus.Rejected)}");

            Console.WriteLine(
                $"Failed: {savedOrders.Count(o => o.Status == OrderStatus.Failed)}");

            if (savedOrders.Count != 20)
            {
                throw new InvalidOperationException(
                    $"Expected 20 orders, found {savedOrders.Count}");
            }

            Console.WriteLine("All 20 orders saved successfully.");

            if (ct.IsCancellationRequested)
            {
                int cancelled = savedOrders.Count(o =>
                    o.Status == OrderStatus.Pending &&
                    o.FailureReason == "Cancelled");

                int finished = savedOrders.Count(o =>
                    o.Status != OrderStatus.Pending);

                Console.WriteLine(
                    $"Run cancelled — {finished} orders finished, " +
                    $"{cancelled} cancelled.");
            }


            Task<Report> reportTask = _reportGenerator.GenerateAsync(ct);

            Console.WriteLine("Generating report...");

            Report report = await reportTask;

            Console.WriteLine("\n===== Sales Report =====");

            Console.WriteLine($"Total Orders: {report.TotalOrders}");
            Console.WriteLine($"Completed: {report.CompletedCount}");
            Console.WriteLine($"Rejected: {report.RejectedCount}");
            Console.WriteLine($"Failed: {report.FailedCount}");
            Console.WriteLine($"Payment Failed: {report.PaymentFailedCount}");

            Console.WriteLine($"Total Revenue: {report.TotalRevenue:F2}");
            Console.WriteLine($"Average Order Value: {report.AverageOrderValue:F2}");

            Console.WriteLine($"Top Selling Product: {report.TopSellingProduct}");
            Console.WriteLine($"Top Selling Quantity: {report.TopSellingQuantity}");

            Console.WriteLine($"Generated At: {report.GeneratedAt}");

        }
    }
}
