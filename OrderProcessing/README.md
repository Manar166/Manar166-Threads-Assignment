
## 1. How to run and project structure

**Prerequisites:** .NET 10 SDK. Target framework in `OrderProcessingSystem.csproj`: `net10.0`; implicit usings and nullable reference types are enabled.

From the directory containing `OrderProcessingSystem.csproj`:

```powershell
dotnet restore
dotnet build
dotnet run
```

The menu offers **Phase 1**, **Phase 2**, **Phase 3**, and **Phase 4**. Alternatively, use:

```powershell
dotnet run -- --phase 1
dotnet run -- --phase 2
dotnet run -- --phase 3
dotnet run -- --phase 4
dotnet run -- --phase 3 --cancel-after 500
dotnet run -- --phase 4 --cancel-after 500
```

`--cancel-after` specifies milliseconds. **Caution:** `DataSeeder.ResetAsync` deletes and recreates the Data directory. Capture JSON results between runs. Phase 4 executes checkout/dashboard/shipping first, then fraud/email re-send; Task 10.4's send test therefore is not deleted by a subsequent Phase 4 reset. This menu and ordering were added in the documentation package and require a build/run check on the target PC.

**Data location:** `JsonFileStore` defaults to `Path.Combine(AppContext.BaseDirectory, "Data")`. For a Debug build this is usually `OrderProcessingSystem/bin/Debug/net10.0/Data/`, *not* the solution root. Invoice files are under `Data/invoices/`; reports under `Data/Reports/`. `ResetAsync()` regenerates this directory.

### Source map and responsibilities

| Folder / class | Responsibility |
|---|---|
| `Program` | Creates dependencies, controls cancellation and dispatches selected phase. |
| `DataSeeder`, `DataFiles` | Seeds customers/products/coupons/20 incoming orders, resets and names JSON files. |
| `JsonFileStore` | JSON serialization, async and sync file operations, atomic replace, per-file synchronization. |
| `Log` | Timestamp/thread-aware simulated repository logging. |
| `Models/Customer`, `Product`, `Coupon`, `Order`, `OrderItem` | Domain records and order lines. |
| `Models/PaymentResult`, `PaymentTransaction`, `EmailMessage`, `Report` | Payment/email/report data. |
| `Enums/LoyaltyTier`, `OrderStatus`, `PaymentStatus` | Domain statuses. |
| `Repos/CustomerRepository`, `ProductRepository`, `CouponRepository` | Async access to master data and simulated I/O delays. |
| `Repos/IncomingOrderRepository`, `OrderRepository` | Incoming and processed order queries and persistence. |
| `Repos/PaymentRepository`, `EmailRepository` | Customer-scoped payment/email lookup. |
| `SyncRepos/*` | Blocking repository counterparts used for comparisons. |
| `NewFolder/OrderValidator`, `SyncOrderValidator` | Async vs sync validation. |
| `Services/OrderCopier`, `OrderPricer`, `PricingService` | Copy/order pricing, coupons, shipping calculation. |
| `Services/OrderProcessorV2` | Phase 2 validated order workflow. |
| `Services/CustomerService`, `InventoryService` | Customer checks, all-or-nothing stock reservation and release. |
| `Services/PaymentGatway`, `OrderProcessor`, `EmailService` | Payment, checkout compensation, retries and email handling. |
| `Services/SyncOrderProcessor` | Blocking order processing used for sync comparison scenarios. |
| `Generators/InvoiceGenerator`, `ReportGenerator` | Invoice text generation and sales reporting. |
| `Runners/Phase1Runner` | Phase 1 sync/async validation measurement. |
| `Runners/RaceConditionDemo`, `Phase2Runner` | Race demonstration, concurrent processing and pricing comparisons. |
| `Runners/Phase3Runner` | Concurrent 20-order checkout and status summary. |
| `Runners/Phase 4/phase4Runner` (`Phase4Runner`) | Task 10.1 channel progress, 10.2 dashboard, 10.3 shipping. |
| `Runners/Phase 4/CustomerDashboard`, `CustomerDashboardService` | Combines profile, orders, payments, email queries. |
| `Interfaces/IShippingCarrier`, `FastShip`, `CairoExpress`, `NileCourier` | Carrier contract and three simulated providers. |
| `Runners/Phase 4/ShippingQuote`, `ShippingQuoteService` | Shipping quote result, first-success race, deadline, cancellation. |
| `Runners/Phase 4/Task104Runner` | Fraud scoring and email resend. |
| `Interfaces/IEmailService`, `IPaymentGateway` | Service contracts. |
| `Exceptions/*` | Customer, coupon, file, shipping, SMTP and write failures. |

## 2. Storage design and JSON integrity

`DataFiles` defines eight JSON files. Every file is a **JSON array**; property names are **camelCase** and enum values are serialized as strings (`JsonStringEnumConverter`). Example shapes below illustrate the schema; field availability depends on the model.

| File | Contents | Uploaded snapshot count |
|---|---|---:|
| `customers.json` | Customer profiles, email, active flag, loyalty tier | 10 |
| `products.json` | Catalog, prices, stock, weights | 8 |
| `coupons.json` | Coupon codes, discount rates and expiry | 4 |
| `incoming_orders.json` | Initial order requests/items | 20 |
| `orders.json` | Saved order statuses, totals and failures | 20 |
| `payments.json` | Payment transaction records | 12 |
| `emails.json` | Sent confirmation email records | 9 |
| `email_retry.json` | Emails pending later retry | 1 |

Example illustrative snippets (not copied transactions):

```json
[
  { "id": 1, "name": "Ahmed Ali", "email": "ahmed.ali@example.com", "isActive": true, "loyaltyTier": "Gold" }
]
```

```json
[
  { "orderId": 3, "status": "Captured" }
]
```

**Concurrent-access protections:** `JsonFileStore` uses a static `ConcurrentDictionary<string, SemaphoreSlim>` to associate a semaphore with each path. Its asynchronous read/write operations acquire that semaphore. Writes serialize to a unique `.tmp` file and use `File.Move(tempPath, filePath, overwrite: true)` to replace the destination only after complete writing; cleanup removes leftovers and releases the semaphore. The store also exposes synchronous per-file `Lock` objects for blocking operations. This design prevents cooperating operations in the same process from interleaving partial JSON writes. Verify that all read-modify-write paths go through the protected `UpdateAsync`/sync counterpart; locking individual reads and writes separately does **not** make a compound transaction atomic. Atomic replacement is not a substitute for multi-process cross-file transactions. Cancellation must not leave invalid JSON.

**Snapshot limitation:** `sample-output/uploaded-snapshot/` is the set of JSON files supplied in the ZIP. It is not evidence that each independent phase passed its end-state checks. Save fresh files into `sample-output/phase-1/` etc. after running each phase and compare against Sections 7.3, 8.4, 9.4 and 10.1 of the assignment.

## 3. Performance and concurrency measurements

*Missing figures were not present in the supplied transcript/ZIP. Record each measurement from a fresh local run using `Stopwatch`, distinct `Environment.CurrentManagedThreadId` values and a snapshot of `ThreadPool.ThreadCount` at the chosen benchmark boundaries. `ThreadPool.ThreadCount` is the size of the pool at observation time, **not** the number of active operations.*

| Experiment | Time | Distinct threads | ThreadPool.ThreadCount | Evidence |
|---|---:|---:|---:|---|
| Phase 1 — sync validation | Not measured | Not measured | Not measured | Runner measures sync elapsed time |
| Phase 1 — async validation | Not measured | Not measured | Not measured | Runner measures async elapsed time |
| Phase 2 — pricing sequential | Not measured | Not measured | Not measured | `Phase2Runner` prints ms |
| Phase 2 — pricing concurrent | Not measured | Not measured | Not measured | `Phase2Runner` prints ms |
| Phase 2 — concurrent order processing | Not measured | Not measured | Not measured | `Phase2Runner` stopwatch |
| Phase 3 — sync (10 dedicated threads) | Not measured | Not measured | Not measured | No verified benchmark output supplied |
| Phase 3 — async (10 tasks) | Not measured | Not measured | Not measured | No verified benchmark output supplied |
| Phase 4 — Dashboard Customer #3 | **802 ms** | Not measured | Not measured | User console result |
| Phase 4 — Shipping Order #1 | **1050 ms** | Not measured | Not measured | User console result: FastShip, free shipping |
| Phase 4 — Shipping Orders #2–#5 | **1208, 1211, 1203, 1201 ms** | Not measured | Not measured | User console results, Flat rate |
| Phase 4 — Fraud sequential | **4.89 s** | Not measured | Not measured | User console result |
| Phase 4 — Fraud parallel | **0.55 s** | **16** | Not measured | User console result |
| Phase 4 — Email re-send | **4.18 s** | Not measured | Not measured | User console result |

**Fraud validation:** 1,005 flags in both runs; first five IDs `4, 8, 12, 29, 33`; identical result sets; speed-up **8.83×**; `Environment.ProcessorCount = 16`. `Parallel.ForEach` has `MaxDegreeOfParallelism = Environment.ProcessorCount`, `ConcurrentBag<int>` ensures thread-safe collection, and cancellation is checked during hashing.

**Email re-send validation:** 30 sends requested with peak concurrency **4** using `Parallel.ForEachAsync(MaxDegreeOfParallelism = 4)`; duration ~4.18s is consistent with the existing ~500ms SMTP simulation over eight waves. Still required: validate `emails.json` contains exactly one message for every test order 1001–1030 and test cancellation. `Interlocked` counters in the runner are instrumentation, not the concurrency limiter.

**Dashboard:** Four independent load tasks start before one `Task.WhenAll` await. Inspect `all.Exception.InnerExceptions` to surface both missing customer 99 and simulated payments-store timeout. A successful Customer #3 has two orders (#3 Completed, #13 Failed), two payments (#3 Captured, #13 Refunded), and one email (#3) according to the assignment target; the user console independently confirmed counts `2 / 2 / 1`.

**Shipping:** Each carrier responds after 150ms handshake plus a carrier-specific random delay; the first successful quote wins, failures are skipped, unfinished calls are cancelled, and a 1200ms deadline returns a 25.00 flat-rate fallback. When all carriers are explicitly injected to fail for #6, the observed output was `All carriers failed`. Observed slight deadline overshoot (1–11ms) includes scheduling and cleanup. Investigate and record cancellation propagation on the caller token separately.

![alt text](/SceenShoots/image.png)
```text


Phase 1 questions 
Why are the sync and async times almost the same when orders are awaited one after another?

The sync and async versions process orders sequentially
In the async version:
 each order must finish validation and saving before the next order starts because we use await inside the foreach loop

Since both versions process orders sequentially and experience similar simulated delays their execution times can be close


2. Is the thread ID before an await always the same as after it? Why? Use your own log lines as evidence.

No. The thread ID before an await is not guaranteed to be the same as the thread ID handles it 

because the thread returns back to the threadpool once we use (await) 
and the OS is the one who reassign a thread from threadPool after method is finished
```

![alt text](/SceenShoots/Screenshot%202026-10-26%20143353.png)

```text
3. What does the thread do while Task.Delay() is waiting? And while Thread.Sleep() is waiting?

Task.Delay()

 the method can suspend without blocking the current thread and The thread can return to the thread pool and perform other work
  When the delay completes the continuation can resume.

Thread.Sleep()

Thread.Sleep() blocks the current thread for the specified time
 That thread cannot execute other work during the sleep 



4. Why should a repository method return Task<T> instead of T?


Returning Task<T> allows the caller to use await without blocking the current thread while waiting for the operation to complete

 It also allows exceptions and cancellation to be handled through the asynchronous operation.



5. What is the difference between File.ReadAllText and File.ReadAllTextAsync? Does the async version occupy a thread while the disk is reading?

File.ReadAllText() reads an entire file synchronously
 The calling thread remains blocked until the read operation completes

File.ReadAllTextAsync() reads the file asynchronously and returns a Task<string>
 When awaited the calling method can suspend while the read is in progress  allowing the thread to do other work.

------------------------------------------------------------------------------------------------------------------------------------
Phase 2 questions



1-What is the difference between starting a task and awaiting it?

Starting a task means initiating an operation and obtaining a Task that represents its progress
 Awaiting a task means asynchronously waiting for it to complete and obtaining its result or exception
-----------------------------------

2-Does every Task create a new thread? Does Task.Delay() use a thread while waiting?


No, a Task does not necessarily create a new thread A Task represents an asynchronous operation which may or may not require a thread during its execution

Task.Delay uses a timer and does not occupy a thread while waiting After the delay completes the continuation can run on an available ThreadPool thread

Task.Run schedules work on the ThreadPool

----------------------------------------
3-What happens if you call .Result or .Wait() on an unfinished task instead of await? Why is it
dangerous? Write a small separate demo method — the only place .Result / .Wait() is allowed.

using .Result() or .Wait() blocks the thread untile the task is finished 

it is dangerous because if every task blocks the thread this will lead to threadPool starvation

```csharp
public static void BlockingDemo()
{
    Console.WriteLine("Starting task...");

    Task task = Task.Delay(3000);

    Console.WriteLine("Waiting for task...");

    task.Wait();

    Console.WriteLine("Task completed.");
}


```
```
```text
------------------------------------------
What happens to an exception thrown inside a task that is never awaited?

When an exception is thrown inside a Task the Task becomes Faulted and stores the exception

If the Task is never awaited or its exception is never observed the error may go unnoticed

 and the application may continue running without handling the failure

-------------------------------------------

When should you use Task.Run(), and when should you avoid it?

Task.Run should be used for CPU-bound operations that require intensive processing such as calculations or rendering

In my project I used Task.Run in InvoiceGenerator to simulate PDF rendering using 20,000,000 iterations

avoid it in I/o bound tasks because if i did that I will be calling a thread from threadPool just to wait 
-------------------------------------------

What is the difference between Task and Task<T>?

Task used with methods with no return 

Task<T> methods return a value
----------------------------------------------

Why can a lock block not contain an await?

because the thread who takes the lock on the object must be the same one who release it 

and using a wait makes the thread go to the threadpool and no grantuee that the same thread will resume the method
-----------------------------------------------
Why did the unprotected stock reservation oversell, and how did you prevent it?

 because two concurrent orders could read the same available stock before either one saved its update

Both orders could see one unit in stock and successfully reserve it
 This is a race condition that can also cause a lost update

I prevented this by using JsonFileStore.UpdateAsync, which protects the entire read-check-modify-write operation with a per-file SemaphoreSlim

This ensures that only one reservation can modify the stock at a time
 In the safe reservation test one order succeeded, the other failed, and the final stock quantity was zero
 ------------------------------------------------

 Task 9.7  Fire-and-forget investigation

 Implement the broken method exactly as given (keep the CS4014 warning inside #pragma warning disable
CS4014):
public async Task ProcessOrderAsync(Order order, Customer customer)
{
    await _orderRepository.SaveAsync(order);

#pragma warning disable CS4014
    _emailService.SendConfirmationAsync(order, customer);
#pragma warning restore CS4014

    Console.WriteLine("Order processing finished");
}
1 Find the problem in this code.

Incorrect completion message: "Order processing finished" may be printed before the email has finished sending.
Exceptions may go unobserved: If sending the email fails, the calling method does not directly observe or handle the failure.
Execution order
SaveAsync() completes because it is awaited.
SendConfirmationAsync() starts, but its returned Task is not awaited.
"Order processing finished" may be printed while the email is still being sent.

______________
Show what happens if the email is still being sent when the method returns, or when the console app
exits.
The email may still be sending when the method finishes.
If the console application exits before the email operation completes, the email may not finish. An unawaited Task does not keep the console application alive by itself.
_________
3 Show what happens if the email operation throws an exception.

The exception faults the email's Task.
ProcessOrderAsync() does not await that task, so the exception is not propagated through ProcessOrderAsync() to its caller.
The caller may believe the order processing finished successfully, even though the email failed.

The email failure might not be observed at all. An unobserved task exception does not necessarily crash the application.
_______________

4 Fix the implementation. Show the console output before and after the fix.
 before


public async Task ProcessOrderAsync(Order order, Customer customer)
{
await _orderRepository.SaveAsync(order);
await _emailService.SendConfirmationAsync(order, customer); // awaited
Console.WriteLine("Order processing finished");
}
 

[01:15:57.105] [Thread 19] OrderRepository -> Opening connection...

[01:15:57.307] [Thread 11] OrderRepository -> Executing query for saving order...

[01:15:57.366] [Thread 19] OrderRepository -> Opening connection...

[01:15:57.522] [Thread 18] OrderRepository -> Executing query for saving order...

[01:15:58.379] [Thread 11] OrderRepository -> Executing query for Gettind All Orders.


--------------------------------------------------------------------

after
Order #17: Email attempt 1 failed: SMTP provider unavailable
[01:18:52.495] [Thread 22] OrderRepository -> Opening connection...

[01:18:52.709] [Thread 22] OrderRepository -> Executing query for saving order...

[01:18:52.772] [Thread 22] OrderRepository -> Executing query for saving order...


Order #17: Email attempt 2 failed: SMTP provider unavailable


[01:18:53.038] [Thread 18] OrderRepository -> Opening connection...

Order #17: Email attempt 3 failed: SMTP provider unavailable
Order #17: Email failed after 3 attempts.
Order #3: Email attempt 1 failed: SMTP provider unavailable
Order #3: Email attempt 2 failed: SMTP provider unavailable
[01:18:54.702] [Thread 23] OrderRepository -> openConnection ...


---------------------------------------------------------
1. When is await necessary?
await is necessary when the program needs to ensure that an asynchronous operation finishes before continuing.

It should be used when:
- The next operation depends on the result of the current operation.
- Exceptions must be caught and handled.
- The operation must complete before the method returns.
- The operation is important, such as saving orders, processing payments, or sending confirmation email

2. When is fire-and-forget acceptable (if ever)?
Fire-and-forget may be acceptable for non-critical background operations when their completion is not required by the caller.

Examples include optional telemetry, analytics, or best-effort notifications.

However, the application must still manage exceptions and the operation's lifetime. A safer approach is to enqueue background work in a managed background service that tracks failures and supports graceful shutdown.

Fire-and-forget should not be used for critical operations such as payments, database updates, or essential confirmation emails without reliable background processing.
3. Why is async void dangerous?
async void is dangerous because it does not return a Task.

Therefore:
- The caller cannot await its completion.
- The caller cannot reliably know when it finishes.
- Exceptions cannot be caught by awaiting the operation.
- Unhandled exceptions may crash the application.
- It is difficult to test and coordinate with other asynchronous operation


Why is async void acceptable for event handlers?

async void is acceptable for event handlers because standard .NET event delegates, such as EventHandler, require a void return type.

Using async void allows the event handler to perform asynchronous operations without blocking the UI thread, keeping the application responsive.


-----------------------------------------------------------------------
### Task 10.4 — Execution Results

**Part A — CPU-bound Fraud Scoring**

The sequential execution took 4.89 seconds, while the parallel execution took 0.55 seconds, achieving an 8.83x speed-up.

The parallel implementation used 16 distinct threads on a system reporting 16 logical processors.

Both executions flagged exactly 1,005 orders and produced identical order IDs. The first five flagged IDs were 4, 8, 12, 29, and 33.

`Parallel.ForEach` was selected because fraud scoring is CPU-bound, and `MaxDegreeOfParallelism` limits the number of concurrent iterations.

**Part B — I/O-bound Email Re-send**

The system processed 30 email send requests in 4.18 seconds, with a peak concurrency of exactly 4.

`Parallel.ForEachAsync` was selected because sending emails is an asynchronous I/O-bound operation. The library manages concurrency using `MaxDegreeOfParallelism = 4`.

The observed time was shorter than the assignment's approximate 8-second estimate because the email service simulates a 500 ms send rather than a 1-second send.

Email persistence and cancellation still require explicit verification before considering every requirement fully tested.



```

