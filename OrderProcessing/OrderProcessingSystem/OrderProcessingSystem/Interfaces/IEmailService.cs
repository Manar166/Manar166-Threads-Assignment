using OrderProcessingSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Interfaces
{
    public interface IEmailService
    {
        Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct);
        Task SendConfirmationWithRetryAsync(Order order, Customer customer, CancellationToken ct);

    }
}
