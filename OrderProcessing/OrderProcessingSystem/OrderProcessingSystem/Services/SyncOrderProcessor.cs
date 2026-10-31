using OrderProcessingSystem.Enums;
using OrderProcessingSystem.Exceptions;
using OrderProcessingSystem.Interfaces;
using OrderProcessingSystem.Models;
using OrderProcessingSystem.Repos;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Services
{
    public class SyncOrderProcessor
    {
        //public bool SyncProcessOrder(Order order)
        //{
        //    bool isReserved = false; PaymentResult paymentResult = new();
        //    try
        //    {
        //        Customer customer;
        //        try
        //        {
        //            customer = await _customerService.GetCustomerAsync(order.CustomerId, ct);


        //        }
        //        catch (CustomerNotFoundExption ex)
        //        {
        //            order.Status = OrderStatus.Rejected;
        //            order.FailureReason = ex.Message;
        //            await orderRepository.SaveAsync(order, ct);
        //            return false;
        //        }



        //        if (!customer.IsActive)
        //        {
        //            order.Status = OrderStatus.Rejected;
        //            order.FailureReason = "Customer is inactive";
        //            await orderRepository.SaveAsync(order, ct);
        //            return false;

        //        }

        //        isReserved = await inventoryService.ReserveStockAsync(order, ct);
        //        if (!isReserved)
        //        {
        //            order.Status = OrderStatus.Rejected;
        //            order.FailureReason = $"Insufficient stock ";
        //            await orderRepository.SaveAsync(order, ct);
        //            return false;


        //        }

        //        try
        //        {
        //            decimal totalPrice = await pricingService.CalculateTotalAsync(order, ct);
        //        }
        //        catch (InvalidCouponException ex)
        //        {
        //            bool released = await inventoryService.ReleaseStockAsync(order, CancellationToken.None);
        //            if (!released)
        //            {
        //                throw new InvalidOperationException(
        //                    $"Failed to release stock for Order #{order.Id}");
        //            }

        //            isReserved = false;

        //            order.Status = OrderStatus.Rejected;
        //            order.FailureReason = $"{ex.Message} ";
        //            await orderRepository.SaveAsync(order, ct);
        //            return false;


        //        }
        //        catch (ShippingProviderUnavailableException ex)
        //        {
        //            await inventoryService.ReleaseStockAsync(
        //                order, CancellationToken.None);

        //            isReserved = false;

        //            order.Status = OrderStatus.Failed;
        //            order.FailureReason = ex.Message;

        //            await orderRepository.SaveAsync(
        //                order, CancellationToken.None);

        //            return false;
        //        }


        //        using var paymentCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        //        paymentCts.CancelAfter(TimeSpan.FromSeconds(3));



        //        try
        //        {
        //            paymentResult = await paymentGatway.ProcessPaymentAsync(order.Id, order.TotalAmount, paymentCts.Token);

        //            if (!paymentResult.Success)
        //            {


        //                bool released = await inventoryService.ReleaseStockAsync(order, ct);
        //                if (!released)
        //                {
        //                    throw new InvalidOperationException(
        //                        $"Failed to release stock for Order #{order.Id}");
        //                }

        //                order.Status = OrderStatus.PaymentFailed;
        //                order.FailureReason = $"PaymentFailed, Card declined by issuer ";
        //                await orderRepository.SaveAsync(order, ct);
        //                return false;
        //            }

        //            order.TransactionId = paymentResult.TransactionId;
        //            order.Status = OrderStatus.Completed;
        //            order.FailureReason = null;
        //            try
        //            {
        //                await orderRepository.SaveAsync(order, ct);
        //            }
        //            catch (DatabaseWriteException)
        //            {
        //                await paymentGatway.RefundAsync(
        //                    paymentResult.TransactionId,
        //                    CancellationToken.None);

        //                paymentResult.Status = PaymentStatus.Refunded;
        //                bool released = await inventoryService.ReleaseStockAsync(order, CancellationToken.None);
        //                if (!released)
        //                {
        //                    throw new InvalidOperationException(
        //                        $"Failed to release stock for Order #{order.Id}");
        //                }

        //                isReserved = false;

        //                order.Status = OrderStatus.Failed;
        //                order.FailureReason =
        //                    $"Database save failed after payment; payment " +
        //                    $"{order.TransactionId} refunded";

        //                await orderRepository.SaveAsync(
        //                    order, CancellationToken.None);

        //                return false;
        //            }





        //        }

        //        catch (OperationCanceledException) when (!ct.IsCancellationRequested && paymentCts.IsCancellationRequested)

        //        {
        //            bool released = await inventoryService.ReleaseStockAsync(order, CancellationToken.None);
        //            if (!released)
        //            {
        //                throw new InvalidOperationException(
        //                    $"Failed to release stock for Order #{order.Id}");
        //            }

        //            isReserved = false;

        //            order.Status = OrderStatus.PaymentFailed;
        //            order.FailureReason = "Payment gateway timeout (3s)";

        //            await orderRepository.SaveAsync(
        //                order, CancellationToken.None);

        //            return false;
        //        }



        //        bool emailSent = false;

        //        for (int i = 1; i <= 3; i++)
        //        {
        //            ct.ThrowIfCancellationRequested();
        //            try
        //            {
        //                await emailService.SendConfirmationAsync(order, customer, ct);
        //                emailSent = true;
        //                break;
        //            }
        //            catch (SmtpUnavailableException ex)
        //            {
        //                Console.WriteLine($"Order #{order.Id}: Email attempt {i} failed: {ex.Message}");

        //            }


        //        }
        //        if (!emailSent)
        //        {
        //            Console.WriteLine($"Order #{order.Id}: Email failed after 3 attempts.");
        //        }


        //        return order.Status == OrderStatus.Completed;
        //    }
        //    catch (OperationCanceledException) when (ct.IsCancellationRequested)
        //    {
        //        if (paymentResult.Status == PaymentStatus.Captured)
        //        {
        //            await paymentGatway.RefundAsync(paymentResult.TransactionId, CancellationToken.None);
        //        }

        //        if (isReserved)
        //        {
        //            bool released = await inventoryService.ReleaseStockAsync(order, CancellationToken.None);
        //            if (!released)
        //            {
        //                throw new InvalidOperationException(
        //                    $"Failed to release stock for Order #{order.Id}");
        //            }
        //            isReserved = false;
        //        }

        //        order.Status = OrderStatus.Pending;
        //        order.FailureReason = "Cancelled";
        //        await orderRepository.SaveAsync(order, CancellationToken.None);

        //        throw;

        //    }


        //}
    }
}
