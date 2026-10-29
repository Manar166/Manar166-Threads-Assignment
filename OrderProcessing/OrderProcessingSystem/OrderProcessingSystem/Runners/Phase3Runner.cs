
using OrderProcessingSystem;
using OrderProcessingSystem.Models;
using OrderProcessingSystem.Repos;
using OrderProcessingSystem.Services;

public class Phase3Runner
{
    private readonly DataSeeder _dataSeeder;
    private readonly OrderRepository _orderRepository;
    private readonly OrderProcessor _orderProcessor;
    private readonly JsonFileStore _store;

    public Phase3Runner(
        DataSeeder dataSeeder,
        OrderRepository orderRepository,
        OrderProcessor orderProcessor,
        JsonFileStore store)
    {
        _dataSeeder = dataSeeder;
        _orderRepository = orderRepository;
        _orderProcessor = orderProcessor;
        _store = store;
    }

    public async Task RunAsync(CancellationToken ct)
    {
       
        await _dataSeeder.ResetAsync(ct);

        List<Order> orders = await _store.ReadAsync<Order>(
            _store.GetPath(DataFiles.IncomingOrders), ct);

       
        List<Task<bool>> tasks = new();

        foreach (var order in orders)
        {
            if (ct.IsCancellationRequested)
                break;

            tasks.Add(_orderProcessor.ProcessOrderAsync(order, ct));
        }

       
        int finished = 0;
        int cancelled = 0;

        foreach (var task in tasks)
        {
            try
            {
                await task;
                finished++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)

            {
                cancelled++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Processing error: {ex.Message}");

            }
        }

       
        List<Order> savedOrders =
            await _orderRepository.GetAllAsync(CancellationToken.None);

        Console.WriteLine();
        Console.WriteLine($"{"Order",-10} {"Status",-17} {"Total",-12} Reason");


        Console.WriteLine(new string('-', 85));

        foreach (var order in savedOrders.OrderBy(o => o.Id))
        {
            Console.WriteLine(
                $"{order.Id,-10} " +
                $"{order.Status,-17} " +
                $"{order.TotalAmount,-12:F2} " +
                $"{order.FailureReason}");
        }

      
        if (ct.IsCancellationRequested)
        {
            Console.WriteLine(
                $"Run cancelled — {finished} orders finished, " +
                $"{cancelled} cancelled.");
        }
    }
}
