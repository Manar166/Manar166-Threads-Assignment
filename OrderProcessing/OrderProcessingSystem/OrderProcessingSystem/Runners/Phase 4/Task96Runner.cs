
using OrderProcessingSystem;
using OrderProcessingSystem.Models;
using OrderProcessingSystem.Services;
using System.Collections.Concurrent;
using System.Diagnostics;

public class Task96Runner
{
    private readonly DataSeeder dataSeeder;
    private readonly JsonFileStore fileStore;
    private readonly SyncOrderProcessor syncProcessor;
    private readonly OrderProcessor asyncProcessor;

    public Task96Runner(
        DataSeeder dataSeeder,
        JsonFileStore fileStore,
        SyncOrderProcessor syncProcessor,
        OrderProcessor asyncProcessor)
    {
        this.dataSeeder = dataSeeder;
        this.fileStore = fileStore;
        this.syncProcessor = syncProcessor;
        this.asyncProcessor = asyncProcessor;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        // ========== SYNC VERSION ==========

        await dataSeeder.ResetAsync(ct);

        var syncOrders = fileStore
            .SyncRead<Order>(DataFiles.IncomingOrders)
            .Where(o => o.Id >= 1 && o.Id <= 10)
            .OrderBy(o => o.Id)
            .ToList();

        var syncThreadIds = new ConcurrentDictionary<int, byte>();
        var syncErrors = new ConcurrentQueue<Exception>();

        var threads = new List<Thread>();

        var syncWatch = Stopwatch.StartNew();

        foreach (var order in syncOrders)
        {
            var currentOrder = order;

            var thread = new Thread(() =>
            {
                try
                {
                    syncThreadIds.TryAdd(
                        Environment.CurrentManagedThreadId, 0);

                    syncProcessor.ProcessOrder(currentOrder);
                }
                catch (Exception ex)
                {
                    syncErrors.Enqueue(ex);
                }
            });

            threads.Add(thread);
        }

        foreach (Thread thread in threads)
            thread.Start();

        foreach (Thread thread in threads)
            thread.Join();

        syncWatch.Stop();

        if (!syncErrors.IsEmpty)
            throw new AggregateException(syncErrors);

        Console.WriteLine("=== SYNC VERSION ===");
        PrintResults(syncOrders);
        Console.WriteLine(
            $"Time: {syncWatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine(
            $"Distinct threads: {syncThreadIds.Count}");
        Console.WriteLine(
            $"ThreadPool.ThreadCount: {ThreadPool.ThreadCount}");

        // ========== ASYNC VERSION ==========

        await dataSeeder.ResetAsync(ct);

        var asyncOrders = await fileStore.ReadAsync<Order>(
            DataFiles.IncomingOrders, ct);

        asyncOrders = asyncOrders
            .Where(o => o.Id >= 1 && o.Id <= 10)
            .OrderBy(o => o.Id)
            .ToList();

        var asyncWatch = Stopwatch.StartNew();

        var tasks = new List<Task<bool>>();

        foreach (Order order in asyncOrders)
        {
            tasks.Add(
                asyncProcessor.ProcessOrderAsync(order, ct));
        }

        foreach (Task<bool> task in tasks)
        {
            await task;
        }

        asyncWatch.Stop();

        Console.WriteLine();
        Console.WriteLine("=== ASYNC VERSION ===");
        PrintResults(asyncOrders);
        Console.WriteLine(
            $"Time: {asyncWatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine(
            $"ThreadPool.ThreadCount: {ThreadPool.ThreadCount}");
    }

    private static void PrintResults(List<Order> orders)
    {
        foreach (var group in orders.GroupBy(o => o.Status))
        {
            Console.WriteLine(
                $"{group.Key}: {group.Count()}");
        }
    }
}
