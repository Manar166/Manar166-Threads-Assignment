using OrderProcessingSystem.Models;
using OrderProcessingSystem.Repos;

namespace OrderProcessingSystem.Phase_4
{
    public class CustomerDashboardService
    {

        private readonly CustomerRepository customerRepository;
        private readonly OrderRepository orderRepository;
        private readonly PaymentRepository paymentRepository;
        private readonly EmailRepository emailRepository;
        private readonly int[] failPaymentsForCustomerIds;

        public CustomerDashboardService(
            CustomerRepository customerRepository,
            OrderRepository orderRepository,
            PaymentRepository paymentRepository,
            EmailRepository emailRepository,
            int[] failPaymentsForCustomerIds)
        {
            this.customerRepository = customerRepository;
            this.orderRepository = orderRepository;
            this.paymentRepository = paymentRepository;
            this.emailRepository = emailRepository;
            this.failPaymentsForCustomerIds = failPaymentsForCustomerIds;
        }





        public async Task<CustomerDashboard> LoadAsync(
            int customerId,
            CancellationToken ct)
        {
            // Start all four operations concurrently
            Task<Customer> customerTask =
                LoadCustomerAsync(customerId, ct);

            Task<List<Order>> ordersTask =
                orderRepository.GetByCustomerIdAsync(customerId, ct);

            Task<List<PaymentTransaction>> paymentsTask =
                LoadPaymentsAsync(customerId, ct);

            Task<List<EmailMessage>> emailsTask =
                emailRepository.GetEmailsByCustomer(customerId, ct);

            // One await for all four loads
            Task all = Task.WhenAll(
                customerTask,
                ordersTask,
                paymentsTask,
                emailsTask);

            try
            {
                await all;
            }
            catch
            {
                if (all.IsFaulted && all.Exception != null)
                {
                    throw new AggregateException(
                        all.Exception.InnerExceptions);
                }

                throw;
            }

            return new CustomerDashboard
            {
                Customer = customerTask.Result,
                Orders = ordersTask.Result,
                Payments = paymentsTask.Result,
                Emails = emailsTask.Result
            };
        }


        private async Task<Customer> LoadCustomerAsync(
    int customerId,
    CancellationToken ct)
        {
            Customer? customer =
                await customerRepository.GetByIdAsync(customerId, ct);

            return customer ?? throw new Exception(
                $"Customer #{customerId} not found");
        }

        private async Task<List<PaymentTransaction>> LoadPaymentsAsync(
    int customerId,
    CancellationToken ct)
        {
            List<PaymentTransaction> payments =
                await paymentRepository.GetPaymentByCustomer(customerId, ct);

            if (failPaymentsForCustomerIds.Contains(customerId))
            {
                throw new TimeoutException("Payments store timeout");
            }

            return payments;
        }
    }
}
