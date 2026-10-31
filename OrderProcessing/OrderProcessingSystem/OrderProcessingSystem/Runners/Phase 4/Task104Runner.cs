
using OrderProcessingSystem.Interfaces;
using OrderProcessingSystem.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

public class Task104Runner
{
    private readonly IEmailService emailService;

    public Task104Runner(IEmailService emailService)
    {
        this.emailService = emailService;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        Console.WriteLine("===== TASK 10.4 A: FRAUD SCORING =====");

        RunFraudScoring(ct);

        Console.WriteLine();
        Console.WriteLine("===== TASK 10.4 B: EMAIL RE-SEND =====");

        await RunEmailResendAsync(ct);
    }

    private static int Score(
        int orderId,
        CancellationToken ct = default)
    {
        long h = orderId;

        for (int i = 0; i < 100_000; i++)
        {
            if ((i & 1023) == 0)
                ct.ThrowIfCancellationRequested();

            h = (h * 31 + i) % 1_000_000_007;
        }

        return (int)(h % 100);
    }

    private void RunFraudScoring(CancellationToken ct)
    {
        int[] orderIds = Enumerable.Range(1, 10_000).ToArray();

        // ---------- Sequential ----------
        List<int> sequentialFlagged = new();

        var swSequential = Stopwatch.StartNew();

        foreach (int orderId in orderIds)
        {
            ct.ThrowIfCancellationRequested();

            if (Score(orderId, ct) >= 90)
                sequentialFlagged.Add(orderId);
        }

        swSequential.Stop();

        // ---------- Parallel ----------
        var parallelFlagged = new ConcurrentBag<int>();
        var threadIds = new ConcurrentDictionary<int, byte>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = ct
        };

        var swParallel = Stopwatch.StartNew();

        Parallel.ForEach(orderIds, options, orderId =>
        {
            int threadId = Environment.CurrentManagedThreadId;

            threadIds.TryAdd(threadId, 0);

            int score = Score(orderId, ct);

            if (score >= 90)
                parallelFlagged.Add(orderId);
        });

        swParallel.Stop();

        // ---------- Validation ----------
        int[] sequentialIds = sequentialFlagged
            .OrderBy(id => id)
            .ToArray();

        int[] parallelIds = parallelFlagged
            .OrderBy(id => id)
            .ToArray();

        bool sameResults = sequentialIds.SequenceEqual(parallelIds);

        int distinctCount = parallelIds.Distinct().Count();

        double speedup =
            swSequential.Elapsed.TotalSeconds /
            swParallel.Elapsed.TotalSeconds;

        Console.WriteLine($"Sequential flagged: {sequentialIds.Length}");
        Console.WriteLine($"Parallel flagged: {parallelIds.Length}");

        Console.WriteLine(
            $"Sequential time: {swSequential.Elapsed.TotalSeconds:F2} s");

        Console.WriteLine(
            $"Parallel time: {swParallel.Elapsed.TotalSeconds:F2} s");

        Console.WriteLine($"Speed-up: {speedup:F2}x");

        Console.WriteLine($"Distinct threads used: {threadIds.Count}");
        Console.WriteLine($"Processor count: {Environment.ProcessorCount}");

        Console.WriteLine($"Same IDs: {sameResults}");
        Console.WriteLine($"Distinct flagged IDs: {distinctCount}");

        Console.WriteLine(
            $"First five: {string.Join(", ", parallelIds.Take(5))}");

        if (!sameResults ||
            parallelIds.Length != 1005 ||
            distinctCount != 1005 ||
            !parallelIds.Take(5).SequenceEqual(
                new[] { 4, 8, 12, 29, 33 }))
        {
            throw new InvalidOperationException(
                "Fraud scoring validation failed.");
        }
    }

    private async Task RunEmailResendAsync(CancellationToken ct)
    {
        // 30 test orders, IDs 1001–1030.
        int[] orderIds = Enumerable.Range(1001, 30).ToArray();

        // Must use an EmailService without injected failures.
        // All emails use this test customer.
        var customer = new Customer
        {
            Id = 1,
            Name = "Test Customer",
            Email = "test@example.com"
        };

        int currentSends = 0;
        int maxConcurrentSends = 0;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 4,
            CancellationToken = ct
        };

        var sw = Stopwatch.StartNew();

        await Parallel.ForEachAsync(orderIds, options,
            async (orderId, token) =>
            {
                var order = new Order
                {
                    Id = orderId,
                    CustomerId = customer.Id
                };

                int active = Interlocked.Increment(ref currentSends);

                // Update peak concurrency.
                int observedMax;

                do
                {
                    observedMax = Volatile.Read(
                        ref maxConcurrentSends);

                    if (active <= observedMax)
                        break;
                }
                while (Interlocked.CompareExchange(
                    ref maxConcurrentSends,
                    active,
                    observedMax) != observedMax);

                try
                {
                    await emailService.SendConfirmationAsync(
                        order, customer, token);
                }
                finally
                {
                    Interlocked.Decrement(ref currentSends);
                }
            });

        sw.Stop();

        Console.WriteLine("Emails requested: 30");
        Console.WriteLine($"Peak concurrent sends: {maxConcurrentSends}");
        Console.WriteLine($"Total time: {sw.Elapsed.TotalSeconds:F2} s");

        if (maxConcurrentSends != 4)
        {
            throw new InvalidOperationException(
                $"Expected peak concurrency 4, got {maxConcurrentSends}.");
        }
    }
}
