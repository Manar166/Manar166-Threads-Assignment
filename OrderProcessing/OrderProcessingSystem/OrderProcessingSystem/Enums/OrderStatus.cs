using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Enums
{
    public enum OrderStatus
    {
        Pending,
        Validated,
        Rejected,
        PaymentFailed,
        Completed,
        Failed
    }
}
