using OrderProcessingSystem.Classes;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Generators
{
    public class InvoiceGenerator
    {
        private readonly JsonFileStore fileStore;

        public InvoiceGenerator(JsonFileStore fileStore)
        {
            this.fileStore = fileStore;
        }

        public async Task GenerateAsync(Order order, CancellationToken ct)
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < 20000000; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    // Simulate CPU-intensive rendering work
                    Thread.SpinWait(20);

                }
            }
            ,ct);

             string invoiceContent = $"""
                INVOICE
                -------------------------
                Order ID: {order.Id}
                Customer ID: {order.CustomerId}
                Subtotal: {order.SubTotal:F2}
                Discount: {order.Discount:F2}
                Shipping: {order.ShippingCost:F2}
                Total: {order.TotalAmount:F2}
                """;

             string invoicePath = Path.Combine(fileStore.DataDirectory, "invoices", $"invoice_{order.Id}.txt");

            await File.WriteAllTextAsync(
                invoicePath,
                invoiceContent,
                ct);


        }
    }
}
 