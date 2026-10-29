using OrderProcessingSystem.Models;
using OrderProcessingSystem.Enums;
using OrderProcessingSystem.Repos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderProcessingSystem.Generators
{
    
    public class ReportGenerator
    {
        private readonly OrderRepository orderRepository;
        private readonly ProductRepository productRepository;
        private readonly JsonFileStore fileStore;

        public ReportGenerator(ProductRepository productRepository, OrderRepository orderRepository, JsonFileStore fileStore)
        {
            this.productRepository = productRepository;
            this.orderRepository = orderRepository;
            this.fileStore = fileStore;
        }

        public async Task<Report> GenerateAsync(CancellationToken ct)
        {
          var orders= await orderRepository.GetAllAsync(ct);

            var completedOrders = orders.Where(o => o.Status == Enums.OrderStatus.Completed);
            var rejectedOrders = orders.Where(o => o.Status == Enums.OrderStatus.Rejected);
            var failedOrders = orders.Where(o => o.Status == Enums.OrderStatus.Failed);
            var PaymentFailedOrders = orders.Where(o => o.Status == Enums.OrderStatus.PaymentFailed);

            var totalRevenue = completedOrders.Sum(o => o.TotalAmount);

            var averageOrderValue = totalRevenue / completedOrders.Count();

            var TopSellingProduct = completedOrders.SelectMany(o => o.Items).GroupBy(o => o.ProductId)
                                                  .Select(group => new { ProductId = group.Key, Quantity = group.Sum(o => o.Quantity) })
                                                  .OrderByDescending(o => o.Quantity).ThenBy(o => o.ProductId).FirstOrDefault();


            string topSellingName = "";
            if (TopSellingProduct is not null)
            {
              var TopSell= await productRepository.GetByIdAsync(TopSellingProduct.ProductId, ct);
                topSellingName = TopSell.Name;

            }
            var report = new Report
            {
                TotalOrders = orders.Count,
                TotalRevenue = totalRevenue,
                CompletedCount = completedOrders.Count(),
                RejectedCount = rejectedOrders.Count(),
                 
                FailedCount = failedOrders.Count()
                 ,
                PaymentFailedCount = PaymentFailedOrders.Count(),
                 
                TopSellingProduct = topSellingName,
                TopSellingQuantity = TopSellingProduct.Quantity,
                AverageOrderValue = averageOrderValue,
                GeneratedAt = DateTime.Now
            };

            string reportPath = Path.Combine(fileStore.DataDirectory, "Reports", $"report_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };

            string json = JsonSerializer.Serialize(report, options);

            // Step 8: Save report
            await File.WriteAllTextAsync(reportPath, json, ct);

            return report;
        }
    }
}
