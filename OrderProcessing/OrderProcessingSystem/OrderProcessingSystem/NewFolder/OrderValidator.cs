using OrderProcessingSystem.Classes;
using OrderProcessingSystem.Enums;
using OrderProcessingSystem.Repos;
using System.Xml.Linq;

namespace OrderProcessingSystem.NewFolder
{
    public class OrderValidator
    {
        private readonly CustomerRepository _customerRepository;
        private readonly ProductRepository _productRepository;
        private readonly CouponRepository _couponRepository;
        private readonly OrderRepository _orderRepository;

        public OrderValidator(CustomerRepository customerRepository,
                            ProductRepository productRepository,
                            CouponRepository couponRepository,
                            OrderRepository orderRepository
                           )
        {
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            _couponRepository = couponRepository;
            _orderRepository = orderRepository;
            
        }
        public async Task ValidateOrderAsync(Order order,CancellationToken token)
        {
          var customer= await _customerRepository.GetByIdAsync(order.CustomerId, token);
            if (customer is null)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = $"Customer #{order.CustomerId} not found";
                await _orderRepository.SaveAsync(order, token);
                return;
            }
            if (!customer.IsActive)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = " $Customer is InActive";

                await _orderRepository.SaveAsync(order, token);
                return;

            }
            
            foreach (var item in order.Items)
            {
                var product=await _productRepository.GetByIdAsync(item.ProductId,token);


                if (product is null)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Product #{item.ProductId} not found";

                    await _orderRepository.SaveAsync(order, token);

                    return;
                }
            }

            foreach (var item in order.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId, token);


                if (product.StockQuantity<item.Quantity)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Insufficient stock for '{product.Name}";

                    await _orderRepository.SaveAsync(order, token);

                    return;
                }
            }

            if (order.CouponCode is not null)
            {
                var coupon = await _couponRepository.GetByCodeAsync(order.CouponCode, token);
                if (coupon is null)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Coupon {order.CouponCode} doesn't Exist";

                    await _orderRepository.SaveAsync(order, token);

                    return;

                }

                if (coupon.ExpiryDate < DateTime.Today)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Coupon {order.CouponCode} has Expired";

                    await _orderRepository.SaveAsync(order, token);

                    return;
                }
            }


            order.Status = OrderStatus.Validated;
            order.FailureReason = null;
            await _orderRepository.SaveAsync(order, token);







        }

        
    }
}

