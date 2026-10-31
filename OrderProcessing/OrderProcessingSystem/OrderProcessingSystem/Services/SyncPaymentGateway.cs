
using OrderProcessingSystem.Enums;
using OrderProcessingSystem.Models;

namespace OrderProcessingSystem.Services
{
    public class SyncPaymentGateway
    {
        private readonly JsonFileStore store;
        private readonly HashSet<int> timeoutOrderIds;
        private readonly HashSet<int> declineOrderIds;

        public SyncPaymentGateway(
            JsonFileStore store,
            IEnumerable<int>? timeoutOrderIds = null,
            IEnumerable<int>? declineOrderIds = null,
            bool disableFailures = false)
        {
            this.store = store;

            this.timeoutOrderIds = disableFailures
                ? new HashSet<int>()
                : new HashSet<int>(timeoutOrderIds ?? []);

            this.declineOrderIds = disableFailures
                ? new HashSet<int>()
                : new HashSet<int>(declineOrderIds ?? []);
        }

        // 1. Process payment synchronously
        public PaymentResult ProcessPayment(
            int orderId,
            decimal amount)
        {
            bool success = !declineOrderIds.Contains(orderId);

            int delay = timeoutOrderIds.Contains(orderId)
                ? 5000
                : 700;

            // Blocks the current thread
            Thread.Sleep(delay);

            PaymentTransaction payment = new PaymentTransaction
            {
                TransactionId = "TXN-" +
                    Guid.NewGuid().ToString("N")[..12].ToUpper(),

                OrderId = orderId,
                Amount = amount,

                Status = success
                    ? PaymentStatus.Captured
                    : PaymentStatus.Declined,

                CreatedAt = DateTime.UtcNow
            };

            store.Syncupdate<PaymentTransaction>(
                DataFiles.Payments,
                payments =>
                {
                    payments.Add(payment);
                    return payments;
                });

            return new PaymentResult
            {
                TransactionId = payment.TransactionId,
                Success = success,
                Status = payment.Status,
                Message = success
                    ? "Payment captured"
                    : "Card declined by issuer"
            };
        }

        // 2. Refund payment synchronously
        public void Refund(string transactionId)
        {
            store.Syncupdate<PaymentTransaction>(
                DataFiles.Payments,
                payments =>
                {
                    PaymentTransaction? payment =
                        payments.Find(p =>
                            p.TransactionId == transactionId);

                    if (payment != null &&
                        payment.Status == PaymentStatus.Captured)
                    {
                        payment.Status = PaymentStatus.Refunded;
                    }

                    return payments;
                });
        }
    }
}
