```text


 Problems with the Current Design (R2)
1. The original code used synchronous I/O methods ( `Thread.Sleep`, `File.ReadAllText`, and `File.WriteAllText`), which block the executing thread while waiting for system or network responses

2. Each order was loaded, processed, and written to disk one after another. This creates unnecessary delays when exporting many orders

3. Threads were blocked  during delay operations instead of being returned to the ThreadPool to process other work.




 Changes Made and Why (R4)

--Converted Methods to Async: Replaced `Thread.Sleep` with `await Task.Delay()`, and file I/O operations with `File.ReadAllTextAsync()` and `File.WriteAllTextAsync()`. This prevents thread blocking during I/O bound operations

--Overlapped Execution Pattern: Started all invoice export tasks for pending orders simultaneously into a task list, and then awaited them sequentially (`start tasks and then await them one by one`)

--Maintained Core Business Flow: The sequential sequence inside each task (`load order -> read customer -> write invoice`) and the generated invoice text content were kept identical to preserve exact behavior and output


Before 

Starting invoice export...
Customer files: D:\Simulation\Advanced C#\New folder\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\Data
Invoice output: D:\Simulation\Advanced C#\New folder\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices

Wrote D:\Simulation\Advanced C#\New folder\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices\invoice-1001.txt
Wrote D:\Simulation\Advanced C#\New folder\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices\invoice-1002.txt
Wrote D:\Simulation\Advanced C#\New folder\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices\invoice-1003.txt
Wrote D:\Simulation\Advanced C#\New folder\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices\invoice-1004.txt
Wrote D:\Simulation\Advanced C#\New folder\AdvancedCsharp-Assignment-01-InvoiceExport\InvoiceExport\bin\Debug\net9.0\invoices\invoice-1005.txt
Export finished in 5.9 s


After

Starting invoice export...
Customer files: D:\Simulation\Advanced C#\Assignment 4\Manar166-Threads-Assignment\InvoiceExport\InvoiceExport\InvoiceExport\bin\Debug\net10.0\Data
Invoice output: D:\Simulation\Advanced C#\Assignment 4\Manar166-Threads-Assignment\InvoiceExport\InvoiceExport\InvoiceExport\bin\Debug\net10.0\invoices

Wrote D:\Simulation\Advanced C#\Assignment 4\Manar166-Threads-Assignment\InvoiceExport\InvoiceExport\InvoiceExport\bin\Debug\net10.0\invoices\invoice-1003.txt
Wrote D:\Simulation\Advanced C#\Assignment 4\Manar166-Threads-Assignment\InvoiceExport\InvoiceExport\InvoiceExport\bin\Debug\net10.0\invoices\invoice-1002.txt
Wrote D:\Simulation\Advanced C#\Assignment 4\Manar166-Threads-Assignment\InvoiceExport\InvoiceExport\InvoiceExport\bin\Debug\net10.0\invoices\invoice-1001.txt
Wrote D:\Simulation\Advanced C#\Assignment 4\Manar166-Threads-Assignment\InvoiceExport\InvoiceExport\InvoiceExport\bin\Debug\net10.0\invoices\invoice-1005.txt
Wrote D:\Simulation\Advanced C#\Assignment 4\Manar166-Threads-Assignment\InvoiceExport\InvoiceExport\InvoiceExport\bin\Debug\net10.0\invoices\invoice-1004.txt
Export finished in 1.8 s

```
