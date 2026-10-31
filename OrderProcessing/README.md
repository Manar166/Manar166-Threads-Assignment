

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





```

