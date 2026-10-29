using OrderProcessingSystem.Enums;
using OrderProcessingSystem.Interfaces;
using OrderProcessingSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Transactions;

namespace OrderProcessingSystem.Services
{
    public class PaymentGatway : IPaymentGateway
    {
        private readonly JsonFileStore _store;
        private readonly HashSet<int> _timeoutOrderIds;
        private readonly HashSet<int> _declineOrderIds;

        public PaymentGatway(
            JsonFileStore store,
            HashSet<int> timeoutOrderIds,
            HashSet<int> declineOrderIds)
        {
            _store = store;
            _timeoutOrderIds = timeoutOrderIds;
            _declineOrderIds = declineOrderIds;
        }
        public async Task<PaymentResult> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct)
        {
            bool success = !_declineOrderIds.Contains(orderId);
            int delay = _timeoutOrderIds.Contains(orderId)
           ? 5000
           : 700;
            await Task.Delay(delay, ct);
            PaymentTransaction payment = new PaymentTransaction
            {
                TransactionId = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper(),
                OrderId = orderId,
                Amount = amount,
               Status=success?PaymentStatus.Captured: PaymentStatus.Declined,
               CreatedAt=DateTime.UtcNow
            };

            await _store.UpdateAsync<PaymentTransaction>(_store.GetPath(DataFiles.Payments),
               payments =>
               {
                   payments.Add(payment);
                   return payments;


               }

                , ct);

            return new PaymentResult
            {
                TransactionId = payment.TransactionId,
                Success = success,
             
                Message = success
                ? "Payment captured"
                : "Card declined by issuer",
                Status=payment.Status
            };
        }

        public async Task RefundAsync(string transactionId,CancellationToken ct)
        {
              

            await _store.UpdateAsync<PaymentTransaction>(_store.GetPath(DataFiles.Payments),
                payments => {
                    var payment = payments.Find(p => p.TransactionId == transactionId);
                    if (payment != null &&
                    payment.Status == PaymentStatus.Captured)
                    {
                        payment.Status = PaymentStatus.Refunded;
                    }

                    return payments;

                },ct);
             
              


        }
    }
}
