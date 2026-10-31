using OrderProcessingSystem.Models;
using OrderProcessingSystem.Repos;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Services
{
    public  class InventoryService
    {
        private readonly ProductRepository _productRepository;

        public InventoryService(ProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<bool> ReserveStockAsync(Order order, CancellationToken ct)
        {
            List<OrderItem> reservedItems = new();
            foreach (var item in order.Items)
            {
                bool isReserved=await _productRepository.ReserveStockAsync(item.ProductId,item.Quantity, ct);
                if (isReserved)
                {
                    reservedItems.Add(item);
                }
                else 
                {
                    foreach (var reservedItem in reservedItems)
                    {
                       await _productRepository.ReleaseStockAsync(reservedItem.ProductId, reservedItem.Quantity, CancellationToken.None);
                    }
                    return false;
                }
            }
            return true;

            
        }

        public async Task<bool> ReleaseStockAsync(Order order, CancellationToken ct)
        {
            bool allReleased = true;

            foreach (var item in order.Items)
            {
               bool isReleased= await _productRepository.ReleaseStockAsync(item.ProductId, item.Quantity, ct);
                if (!isReleased)
                {
                     allReleased = false;
                    Console.WriteLine($"Failed to release item with id{item.ProductId}");
                   
                }
               
            }
           
            return allReleased;
        }

    }
}
