```text

Q1. What is `System.Threading.Monitor` used for?

`System.Threading.Monitor` coordinates threads that share data ,
  thread calls `Monitor.Enter` to acquire an exclusive lock on an object
  and `Monitor.Exit` to release it
  While one thread owns that object's monitor, another thread must wait before it can enter a critical section protected by the same object
 `Monitor` also supports thread coordination with methods such as `Wait`, `Pulse`, and `PulseAll`



 Q2. What does the C# `lock` statement compile to?

 it is compiled it Monitor 
 it calls `Monitor.Enter` and guarantees a matching `Monitor.Exit` in a `finally` block. The compiler also tracks whether the lock was successfully acquired, so it does not release a lock that was never taken. The lock expression is evaluated only once.



Q3. List at least two differences between using lock and calling Monitor APIs directly ?

1-Automatic vs. Manual Management: 

The lock statement is a shorthand (syntactic sugar) that automatically handles acquiring and releasing the lock using a hidden try-finally block

 Calling Monitor methods (Monitor.Enter and Monitor.Exit) requires you to manage the entry and exit manually
  which typically means writing an explicit try-finally block to ensure the lock is always released.


2-Advanced Features and Timeouts: 

Monitor provides advanced capabilities that lock does not support natively, such as Monitor.TryEnter 
(which allows you to specify a timeout or check if a lock is available without blocking indefinitely)
 and thread coordination/signaling methods like Monitor.Wait and Monitor.Pulse

 3-Amount of code:
  `lock` is shorter and less error-prone

 Direct monitor use requires more explicit bookkeeping 
 and careful matching of each successful acquisition with a release.


When might a developer use Monitor.TryEnter, Monitor.Pulse, or Monitor.Wait instead of a plain
lock?

A developer would choose to use Monitor.TryEnter, Monitor.Wait, 
or Monitor.Pulse instead of a plain lock when they need advanced control, 
time-outs, or inter-thread signaling that a standard lock statement cannot provide

1. Monitor.TryEnter (Handling Timeouts and Preventing Deadlocks)
When to use: When you want to attempt to acquire a lock, but you do not want the thread to block indefinitely if the resource is currently busy.

Why not a plain lock: A standard lock will wait forever until the lock becomes available. Monitor.TryEnter allows you to specify a timeout period (e.g., Monitor.TryEnter(obj, TimeSpan.FromSeconds(2))) or check instantly if the lock is free. If it cannot acquire the lock, the thread can move on, log an error, or try an alternative path, which helps prevent deadlocks.

2. Monitor.Wait (Conditional Waiting / Producer-Consumer Scenarios)
When to use: When a thread successfully enters a critical section but realizes that a specific condition isn't met yet (e.g., a shared queue is empty, so a consumer thread has nothing to process).

Why not a plain lock: A plain lock only blocks entry into the code block; it cannot put a thread to sleep while holding the lock and automatically release the lock so other threads can update the state. Monitor.Wait temporarily releases the lock and pauses the thread until another thread signals that the condition has changed.

3. Monitor.Pulse or Monitor.PulseAll (Thread Signaling)
When to use: To wake up one (Pulse) or all (PulseAll) threads that are currently sleeping due to a Monitor.Wait call.

Why not a plain lock: A plain lock has no concept of notifying or waking up other threads. Pulse is used by a thread (e.g., a producer adding an item to a queue) to signal waiting threads (e.g., consumers) that new data is available or the state has changed.



Write the same critical section twice: once with `lock`, once with `Monitor.Enter` / `Monitor.Exit`?

withLock
```csharp
private readonly Lock _lock =new();
private int counter;
lock(_lock)
{
  if(counter>0)
  {
    counter++;
  }

}


//----------------------------------
//    with  Mintor 

using System.Threading;

private readonly object _lockObj = new object();
private int counter;

Monitor.Enter(_lockObj);
    try
    {
        
        _counter++;
        Console.WriteLine($"Counter is now: {_counter}");
    }
    finally
    {
        Monitor.Exit(_lockObj);
    }


```



Sources

(https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor)
(https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/statements)
chatgpt


```
