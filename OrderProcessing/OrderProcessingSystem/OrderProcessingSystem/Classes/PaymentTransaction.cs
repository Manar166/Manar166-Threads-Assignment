using OrderProcessingSystem.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Classes
{
    public class PaymentTransaction
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; } 
        public PaymentStatus Status { get; set; }
        public string? TransactionId { get; set; }
        public DateTime CreatedAt { get; set; }

        public PaymentTransaction() { }
    }
}
