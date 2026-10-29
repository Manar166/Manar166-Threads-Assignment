using OrderProcessingSystem.Models;
using OrderProcessingSystem.Enums;
using OrderProcessingSystem.Repos;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.SyncRepos
{
    public class SyncOrderRepositpry
    {
        private readonly JsonFileStore _fileStore;

        public SyncOrderRepositpry(JsonFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        public List<Order> GetAll()
        {
            Log.Info(nameof(SyncOrderRepositpry), "openConnection ...");
            Thread.Sleep(200);
            Log.Info(nameof(SyncOrderRepositpry), $"Executing query for Gettind All Orders.");

            Thread.Sleep(
            Random.Shared.Next(300, 601));

            var orders =  _fileStore.SyncRead<Order>(DataFiles.Orders);
            Log.Success(
            nameof(SyncOrderRepositpry),
            $"Loaded {orders.Count}  order(s).");
            return orders;

        }


        public void Save(Order order)
        {
            Log.Info(nameof(SyncOrderRepositpry), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(SyncOrderRepositpry), "Executing query for saving order...");
            Thread.Sleep(Random.Shared.Next(400, 601));


            _fileStore.Syncupdate<Order>(
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
                }
                );
        }


        public List<Order> GetByStatus(OrderStatus s)
        {
            var allOrders =  GetAll();

            return allOrders.Where(x => x.Status == s).ToList();

        }

    }
}
