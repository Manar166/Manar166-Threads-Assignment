using OrderProcessingSystem.Models;
using OrderProcessingSystem.Enums;
using OrderProcessingSystem.NewFolder;
using OrderProcessingSystem.Repos;
using System.Diagnostics;

namespace OrderProcessingSystem.Runners
{
    public class Phase1Runner
    {
        private readonly SyncOrderValidator _syncValidator;
        private readonly OrderValidator _asyncValidator;
        private readonly OrderRepository _orderRepository;
        JsonFileStore _jsonFileStore;

        public Phase1Runner(SyncOrderValidator syncValidator, 
                            OrderValidator asyncValidator,
                            OrderRepository orderRepository,
                            JsonFileStore jsonFileStore)
        {
            _syncValidator = syncValidator;
            _asyncValidator = asyncValidator;
            _orderRepository = orderRepository;
            _jsonFileStore = jsonFileStore;
        }


        public async Task RunAsync(CancellationToken ct)
        {

            var seedData = new DataSeeder(_jsonFileStore);
            await seedData.ResetAsync(ct);

            var syncOrders = _jsonFileStore.SyncRead<Order>(DataFiles.IncomingOrders);
            Console.WriteLine($"Loaded {syncOrders.Count} orders for Sync Run.");

            Console.WriteLine("\n========== SYNC RUN ==========");

            var syncWatch = Stopwatch.StartNew();

            foreach (var order in syncOrders)
            {
                ct.ThrowIfCancellationRequested();
                _syncValidator.ValidateOrder(order);
            }

            syncWatch.Stop();

            //Async Run

            await seedData.ResetAsync(ct);



            var asyncOrders = await _jsonFileStore.ReadAsync<Order>(DataFiles.IncomingOrders, ct);

            Console.WriteLine($"Loaded {asyncOrders.Count} orders for Async Run.");


            Console.WriteLine("\n========== ASYNC RUN ==========");

            var asyncWatch = Stopwatch.StartNew();


            foreach (var order in asyncOrders)
            {
               await _asyncValidator.ValidateOrderAsync(order, ct);



            }
            asyncWatch.Stop();

            var validOrders =  await _orderRepository.GetByStatusAsync(OrderStatus.Validated, ct);

            var rejectedOrders = await _orderRepository.GetByStatusAsync(OrderStatus.Rejected, ct);

            var allOrders =await _orderRepository.GetAllAsync(ct);

            //await Task.WhenAll(validOrders, rejectedOrders, allOrders);

            Console.WriteLine("\n===== Validation Summary =====");
            Console.WriteLine($"Total Orders: {allOrders.Count}");
            Console.WriteLine($"Validated:    {validOrders.Count}");
            Console.WriteLine($"Rejected:     {rejectedOrders.Count}");

            foreach (var order in rejectedOrders.OrderBy(o => o.Id))
            {
                Console.WriteLine($"  - Order #{order.Id}: {order.FailureReason}");

            }

            Console.WriteLine($"Sync Time:  {syncWatch.Elapsed.TotalSeconds:F1} seconds");


            Console.WriteLine($"Async Time: {asyncWatch.Elapsed.TotalSeconds:F1} seconds");


            Console.WriteLine($"orders.json now contains {allOrders.Count} records.");




        }
    }
}
