
using OrderProcessingSystem;
using OrderProcessingSystem.Exceptions;
using OrderProcessingSystem.Models;

public class SyncEmailService
{
    private readonly JsonFileStore store;

    private readonly Dictionary<int, int> failAttempts;
    private readonly Dictionary<int, int> attemptCounts = new();

    private readonly object attemptLock = new();

    public SyncEmailService(
        JsonFileStore store,
        Dictionary<int, int>? failAttempts = null,
        bool disableFailures = false)
    {
        this.store = store;

        this.failAttempts = disableFailures
            ? new Dictionary<int, int>()
            : new Dictionary<int, int>(
                failAttempts ?? new Dictionary<int, int>());
    }

    public void SendConfirmation(
        Order order,
        Customer customer)
    {
        int attempt;

        // Multiple threads may send emails simultaneously.
        lock (attemptLock)
        {
            attemptCounts.TryGetValue(order.Id, out int previous);

            attempt = previous + 1;

            attemptCounts[order.Id] = attempt;
        }

        // Simulate SMTP request synchronously.
        Thread.Sleep(500);

        if (failAttempts.TryGetValue(order.Id, out int failures)
            && attempt <= failures)
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

        store.Syncupdate<EmailMessage>(
            DataFiles.Emails,
            emails =>
            {
                emails.Add(email);
                return emails;
            });

        Console.WriteLine(
            $"Email for Order #{order.Id} sent on attempt {attempt}");
    }
}
