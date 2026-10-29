
using OrderProcessingSystem.Models;
using OrderProcessingSystem.Interfaces;
using OrderProcessingSystem.Exceptions;
using OrderProcessingSystem.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderProcessingSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly JsonFileStore _store;
        private readonly Dictionary<int, int> _failAttempts;

        private readonly Dictionary<int, int> _attemptCounts = new();
        private readonly object _lock = new();

        public EmailService(
            JsonFileStore store,
            Dictionary<int, int> failAttempts)
        {
            _store = store;
            _failAttempts = failAttempts;
        }

        public async Task SendConfirmationAsync(
            Order order,
            Customer customer,
            CancellationToken ct)
        {
            int attempt;

            lock (_lock)
            {
                _attemptCounts.TryGetValue(
                    order.Id, out int previous);

                attempt = previous + 1;
                _attemptCounts[order.Id] = attempt;
            }

            // Simulate SMTP request
            await Task.Delay(500, ct);

            if (_failAttempts.TryGetValue(
                    order.Id, out int failures) &&
                attempt <= failures)
            {
                throw new SmtpUnavailableException(
                    "SMTP provider unavailable");
            }

            EmailMessage email = new EmailMessage
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                To = customer.Email,
                Subject = $"Order #{order.Id} Confirmation",
                Body = $"Your order #{order.Id} has been confirmed.",
                SentAt = DateTime.UtcNow,
                Attempts = attempt,
                LastError = null
            };

            await _store.UpdateAsync<EmailMessage>(
                _store.GetPath(DataFiles.Emails),
                emails =>
                {
                    emails.Add(email);
                    return emails;
                },
                ct);
        }
    }
}
