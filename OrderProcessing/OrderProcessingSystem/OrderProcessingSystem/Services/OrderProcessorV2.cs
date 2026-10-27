using OrderProcessingSystem.Classes;
using OrderProcessingSystem.Enums;
using OrderProcessingSystem.Generators;
using OrderProcessingSystem.Repos;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Services
{
    public class OrderProcessorV2
    {
        private readonly ProductRepository _productRepository;
        private readonly OrderPricer _orderPricer;
        private readonly OrderRepository _orderRepository;
        private readonly InvoiceGenerator _invoiceGenerator;

        public OrderProcessorV2(
            ProductRepository productRepository,
            OrderPricer orderPricer,
            OrderRepository orderRepository,
            InvoiceGenerator invoiceGenerator)
        {
            _productRepository = productRepository;
            _orderPricer = orderPricer;
            _orderRepository = orderRepository;
            _invoiceGenerator = invoiceGenerator;
        }
        public async Task<bool> ProcessValidatedOrderAsync(Order order,CancellationToken ct)
        {
            var reservedItems = new List<OrderItem>();
            foreach (var item in order.Items)
            {
                bool isReserved= await _productRepository.ReserveStockAsync(item.ProductId,item.Quantity, ct);

                if (!isReserved)
                {
                    foreach (var reservedItem in reservedItems)
                    {
                        await _productRepository.ReleaseStockAsync(reservedItem.ProductId, reservedItem.Quantity, ct);
                    }

                    reservedItems.Clear();

                    var product = await _productRepository.GetByIdAsync(item.ProductId, ct);
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Insufficient stock for '{product?.Name}'";
                    await _orderRepository.SaveAsync(order, ct);


                    return false;
                }

                
                    reservedItems.Add(item);
                
            }

            await _orderPricer.PriceOrderAsync(order, ct);


            await _invoiceGenerator.GenerateAsync(order,ct);



            order.Status = OrderStatus.Completed;
            await _orderRepository.SaveAsync(order, ct);

            return true;

        }
    }
}
