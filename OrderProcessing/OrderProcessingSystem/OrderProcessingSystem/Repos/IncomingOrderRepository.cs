using OrderProcessingSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Repos
{
    public class IncomingOrderRepository
    {
        private readonly JsonFileStore _fileStore;

        public IncomingOrderRepository(JsonFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        public async Task<List<Order>> GetAllAsync(CancellationToken token)
        {
            Log.Info(nameof(IncomingOrderRepository), "openConnection ...");
            await Task.Delay(200, token);
            Log.Info(nameof(IncomingOrderRepository), $"Executing query for Gettind All Orders.");

            await Task.Delay(Random.Shared.Next(300, 601), token);

            var incomingOrders= await _fileStore.ReadAsync<Order>(DataFiles.IncomingOrders, token);
            Log.Success(
             nameof(IncomingOrderRepository),
             $"Loaded {incomingOrders.Count} incoming order(s).");

            return incomingOrders;

        }
    }
}
