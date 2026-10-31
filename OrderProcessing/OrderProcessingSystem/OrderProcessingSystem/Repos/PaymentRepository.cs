using OrderProcessingSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Repos
{
    public class PaymentRepository
    {
        private readonly JsonFileStore _fileStore;
        private readonly OrderRepository orderRepository;

        public PaymentRepository(JsonFileStore fileStore, OrderRepository orderRepository)
        {
            _fileStore = fileStore;
            this.orderRepository = orderRepository;
        }

        public async Task<List<PaymentTransaction>> GetPaymentByCustomer(int customerId, CancellationToken ct)
        {
          var allPayments=  await _fileStore.ReadAsync<PaymentTransaction>(DataFiles.Payments, ct);
            var customerOrders = await orderRepository.GetByCustomerIdAsync(customerId, ct);
            var customerOrdersIds = customerOrders.Select(o => o.Id).ToList();
            return allPayments.Where(o => customerOrdersIds.Contains(o.OrderId)).ToList();

        }
    }
}
