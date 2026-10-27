using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace InvoiceExport;

public sealed class InvoiceExportService
{
    readonly OrderDatabase _db;
    readonly CustomerFileStore _customers;
    readonly InvoiceFileWriter _invoices;

    public InvoiceExportService(OrderDatabase db, CustomerFileStore customers, InvoiceFileWriter invoices)
    {
        _db = db;
        _customers = customers;
        _invoices = invoices;
    }

    public async Task ExportAllPendingAsync()
    {
        var watch = Stopwatch.StartNew();
        List<Order> orders =  await _db.GetPendingOrdersAsync();

        List<Task> tasks = new List<Task>();

        foreach (Order order in orders)
        {
            tasks.Add(ProcessOrderAsync(order.Id));
        }

        foreach (var task in tasks)
        {
            await task;
        }

        Console.WriteLine($"Export finished in {watch.Elapsed.TotalSeconds:F1} s");

       

    }

    public async Task ProcessOrderAsync(int orderId)
    {
        Order fresh = await _db.GetOrderByIdAsync(orderId)
            ?? throw new InvalidOperationException($"Order {orderId} missing");

        CustomerProfile customer = await _customers.ReadProfileAsync(fresh.CustomerId);
        string path = await _invoices.WriteInvoiceAsync(fresh, customer);
        Console.WriteLine($"Wrote {path}");
    }
}
