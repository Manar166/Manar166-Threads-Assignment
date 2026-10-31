using OrderProcessingSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Repos
{
    public class EmailRepository
    {
        private readonly JsonFileStore _fileStore;
        private readonly OrderRepository orderRepository;

        public EmailRepository(JsonFileStore fileStore, OrderRepository orderRepository)
        {
            _fileStore = fileStore;
            this.orderRepository = orderRepository;
        }

        public async Task<List<EmailMessage>> GetEmailsByCustomer(int customerId, CancellationToken ct)
        {
            var allEmails = await _fileStore.ReadAsync<EmailMessage>(DataFiles.Emails, ct);
            var customerOrders = await orderRepository.GetByCustomerIdAsync(customerId, ct);
            var customerOrdersIds = customerOrders.Select(o => o.Id).ToList();
            return allEmails.Where(o => customerOrdersIds.Contains(o.OrderId)).ToList();

        }
    }
}
