using OrderProcessingSystem.Classes;
using OrderProcessingSystem.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Repos
{
    public class OrderRepository
    {
        private readonly JsonFileStore _fileStore;

        public OrderRepository(JsonFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        public async Task<List<Order>> GetAllAsync(CancellationToken token)
        {
            Log.Info(nameof(OrderRepository), "openConnection ...");
            await Task.Delay(200, token);
            Log.Info(nameof(OrderRepository), $"Executing query for Gettind All Orders.");

            await Task.Delay(Random.Shared.Next(300, 601), token);

            var orders= await _fileStore.ReadAsync<Order>(DataFiles.Orders, token);
            Log.Success(
            nameof(OrderRepository),
            $"Loaded {orders.Count}  order(s).");
            return orders;

        }


        public async Task SaveAsync(Order order, CancellationToken token)
        {
            Log.Info(nameof(OrderRepository), "Opening connection...");
            await Task.Delay(200, token);

            Log.Info(nameof(OrderRepository), "Executing query for saving order...");
            await Task.Delay(Random.Shared.Next(400, 601), token);

            await _fileStore.UpdateAsync<Order>(
                DataFiles.Orders,
                orders =>
                {
                    var orderFromFile = orders.Find(x => x.Id == order.Id);

                    if (orderFromFile is null)
                    {
                        orders.Add(order);
                        return orders;
                    }

                    var index = orders.IndexOf(orderFromFile);
                    orders[index] = order;

                    return orders;
                },
                token);
        }


        public async Task<List<Order>> GetByStatusAsync(OrderStatus s, CancellationToken token)
        {
            var allOrders =await GetAllAsync(token);

            return allOrders.Where(x => x.Status == s).ToList();

        }

    }
}
