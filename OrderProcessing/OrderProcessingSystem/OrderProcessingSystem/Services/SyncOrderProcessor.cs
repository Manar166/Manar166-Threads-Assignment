
using OrderProcessingSystem.Enums;
using OrderProcessingSystem.Exceptions;
using OrderProcessingSystem.Models;
using OrderProcessingSystem.SyncRepos;

namespace OrderProcessingSystem.Services
{
    public class SyncOrderProcessor
    {
        private readonly SyncCustomerRepository customerRepository;
        private readonly SyncInventoryService inventoryService;
        private readonly SyncPricingService pricingService;
        private readonly SyncOrderRepositpry orderRepository;
        private readonly SyncPaymentGateway paymentGateway;
        private readonly SyncEmailService emailService;

        public SyncOrderProcessor(
            SyncCustomerRepository customerRepository,
            SyncInventoryService inventoryService,
            SyncPricingService pricingService,
            SyncOrderRepositpry orderRepository,
            SyncPaymentGateway paymentGateway,
            SyncEmailService emailService)
        {
            this.customerRepository = customerRepository;
            this.inventoryService = inventoryService;
            this.pricingService = pricingService;
            this.orderRepository = orderRepository;
            this.paymentGateway = paymentGateway;
            this.emailService = emailService;
        }

        public bool ProcessOrder(Order order)
        {
            // 1. Validate customer
            Customer? customer =
                customerRepository.GetById(order.CustomerId);

            if (customer == null)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason =
                    $"Customer #{order.CustomerId} not found";

                orderRepository.Save(order);
                return false;
            }

            if (!customer.IsActive)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = "Customer is inactive";

                orderRepository.Save(order);
                return false;
            }

            // 2. Reserve stock
            bool reserved = inventoryService.ReserveStock(order);

            if (!reserved)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = "Insufficient stock";

                orderRepository.Save(order);
                return false;
            }

            // 3. Calculate total
            try
            {
                pricingService.CalculateTotal(order);
            }
            catch (InvalidCouponException ex)
            {
                ReleaseReservedStock(order);

                order.Status = OrderStatus.Rejected;
                order.FailureReason = ex.Message;

                orderRepository.Save(order);
                return false;
            }
            catch (ShippingProviderUnavailableException ex)
            {
                ReleaseReservedStock(order);

                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;

                orderRepository.Save(order);
                return false;
            }

            // 4. Process payment
            PaymentResult payment =
                paymentGateway.ProcessPayment(
                    order.Id, order.TotalAmount);

            if (!payment.Success)
            {
                ReleaseReservedStock(order);

                order.Status = OrderStatus.PaymentFailed;
                order.FailureReason = "Card declined by issuer";

                orderRepository.Save(order);
                return false;
            }

            // 5. Payment captured
            order.TransactionId = payment.TransactionId;
            order.Status = OrderStatus.Completed;
            order.FailureReason = null;

            // 6. Save order + compensation
            try
            {
                orderRepository.Save(order);
            }
            catch (DatabaseWriteException)
            {
                if (string.IsNullOrEmpty(payment.TransactionId))
                    throw;

                paymentGateway.Refund(payment.TransactionId);

                ReleaseReservedStock(order);

                order.Status = OrderStatus.Failed;
                order.FailureReason =
                    $"Database save failed after payment; payment " +
                    $"{payment.TransactionId} refunded";

                orderRepository.Save(order);
                return false;
            }

            // 7. Email confirmation
            try
            {
                emailService.SendConfirmation(order, customer);
            }
            catch (SmtpUnavailableException ex)
            {
                // Email failure must not change Completed.
                Console.WriteLine(
                    $"Order #{order.Id}: Email failed: {ex.Message}");
            }

            return order.Status == OrderStatus.Completed;
        }

        private void ReleaseReservedStock(Order order)
        {
            bool released = inventoryService.ReleaseStock(order);

            if (!released)
            {
                throw new InvalidOperationException(
                    $"Failed to release stock for Order #{order.Id}");
            }
        }
    }
}
