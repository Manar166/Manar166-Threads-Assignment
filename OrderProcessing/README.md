

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
---------------------------------------------
Why did the unprotected stock reservation oversell, and how did you prevent it?

 because two concurrent orders could read the same available stock before either one saved its update

Both orders could see one unit in stock and successfully reserve it
 This is a race condition that can also cause a lost update

I prevented this by using JsonFileStore.UpdateAsync, which protects the entire read-check-modify-write operation with a per-file SemaphoreSlim

This ensures that only one reservation can modify the stock at a time
 In the safe reservation test one order succeeded, the other failed, and the final stock quantity was zero
```

