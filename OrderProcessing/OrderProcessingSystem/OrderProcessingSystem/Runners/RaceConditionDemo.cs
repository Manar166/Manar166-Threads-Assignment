using OrderProcessingSystem.Models;
using OrderProcessingSystem.Repos;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Runners
{
    public class RaceConditionDemo
    {
        private readonly JsonFileStore _store;
        private readonly ProductRepository _productRepository;

        public RaceConditionDemo(
            JsonFileStore store,
            ProductRepository productRepository)
        {
            _store = store;
            _productRepository = productRepository;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            var token = CancellationToken.None;

             await _store.UpdateAsync<Product>(DataFiles.Products,
                            products =>
                            {
                                var product = products.Find(x => x.Id == 4);
                                if (product is null)
                                    throw new InvalidOperationException(
                                        "Product P4 (Keyboard) was not found.");
                                product.StockQuantity = 1;
                                return products;
                            }

                                 , token);

            Console.WriteLine("\n=== Race Condition Demo ===");
            Console.WriteLine("P4 (Keyboard) stock initialized to 1.");

            Task<bool> order10 = _productRepository
                          .ReserveStockAsync(4, 1, token);

            Task<bool> order11 = _productRepository
                .ReserveStockAsync(4, 1, token);

            bool[] results = await Task.WhenAll(order10, order11);

            var products = await _store.ReadAsync<Product>(DataFiles.Products, token);


            var keyboard = products.Find(p => p.Id == 4);

            Console.WriteLine($"Order #10 reservation: {results[0]}");
            Console.WriteLine($"Order #11 reservation: {results[1]}");
            Console.WriteLine($"Final keyboard stock: {keyboard?.StockQuantity}");


        }
    }
}
