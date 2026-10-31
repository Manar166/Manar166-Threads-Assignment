
using OrderProcessingSystem.Enums;
using OrderProcessingSystem.Exceptions;
using OrderProcessingSystem.Models;
using OrderProcessingSystem.SyncRepos;

namespace OrderProcessingSystem.Services
{
    public class SyncPricingService
    {
        private readonly SyncProductRepository productRepository;
        private readonly SyncCustomerRepository customerRepository;
        private readonly SyncCouponRepository couponRepository;

        private readonly HashSet<int> failShippingForOrderIds;

        public SyncPricingService(
            SyncProductRepository productRepository,
            SyncCustomerRepository customerRepository,
            SyncCouponRepository couponRepository,
            IEnumerable<int>? failShippingForOrderIds = null,
            bool disableFailures = false)
        {
            this.productRepository = productRepository;
            this.customerRepository = customerRepository;
            this.couponRepository = couponRepository;

            this.failShippingForOrderIds = disableFailures
                ? new HashSet<int>()
                : new HashSet<int>(
                    failShippingForOrderIds ?? []);
        }

        // 1. Calculate subtotal
        public decimal CalculateSubTotal(Order order)
        {
            decimal subtotal = 0m;

            foreach (var item in order.Items)
            {
                Product? product =
                    productRepository.GetById(item.ProductId);

                if (product is null)
                {
                    return 0m;
                }

                item.UnitPrice = Math.Round(
                    product.Price,
                    2,
                    MidpointRounding.AwayFromZero);

                subtotal += product.Price * item.Quantity;
            }

            order.SubTotal = Math.Round(
                subtotal,
                2,
                MidpointRounding.AwayFromZero);

            return order.SubTotal;
        }

        // 2. Calculate discount
        public decimal GetDiscount(Order order)
        {
            decimal couponDiscount = 0m;

            Customer? customer =
                customerRepository.GetById(order.CustomerId);

            if (customer is null)
            {
                throw new CustomerNotFoundExption(order.CustomerId);
            }

            decimal loyaltyDiscount = customer.LoyaltyTier switch
            {
                LoyaltyTier.Bronze => 0m,
                LoyaltyTier.Silver => 5m,
                LoyaltyTier.Gold => 10m,
                _ => 0m
            };

            if (!string.IsNullOrWhiteSpace(order.CouponCode))
            {
                Coupon? coupon =
                    couponRepository.GetByCode(order.CouponCode);

                if (coupon is null)
                {
                    throw new InvalidCouponException(
                        nameof(order.CouponCode),
                        " does not exist");
                }

                if (coupon.ExpiryDate < DateTime.UtcNow)
                {
                    throw new InvalidCouponException(
                        nameof(order.CouponCode),
                        " has Expired");
                }

                couponDiscount = coupon.DiscountPercent;
            }

            decimal discountPercentage = Math.Min(
                loyaltyDiscount + couponDiscount,
                30m);

            return Math.Round(
                order.SubTotal * discountPercentage / 100m,
                2,
                MidpointRounding.AwayFromZero);
        }

        // 3. Calculate shipping cost
        public decimal CalculateShippingCost(Order order)
        {
            if (failShippingForOrderIds.Contains(order.Id))
            {
                throw new ShippingProviderUnavailableException(
                    "Shipping provider unavailable");
            }

            if (order.SubTotal >= 1000m)
            {
                return 0m;
            }

            decimal totalWeight = 0m;

            foreach (var item in order.Items)
            {
                Product? product =
                    productRepository.GetById(item.ProductId);

                if (product is null)
                {
                    throw new NullReferenceException(
                        "product is null");
                }

                totalWeight += product.WeightKg * item.Quantity;
            }

            int totalQuantity = order.Items.Sum(
                item => item.Quantity);

            decimal shippingCost =
                5.00m +
                1.50m * totalQuantity +
                2.00m * totalWeight;

            return Math.Round(
                shippingCost,
                2,
                MidpointRounding.AwayFromZero);
        }

        // 4. Calculate final total
        public decimal CalculateTotal(Order order)
        {
            CalculateSubTotal(order);

            // Sync operations execute sequentially.
            order.Discount = GetDiscount(order);

            order.ShippingCost =
                CalculateShippingCost(order);

            order.TotalAmount = Math.Round(
                order.SubTotal -
                order.Discount +
                order.ShippingCost,
                2,
                MidpointRounding.AwayFromZero);

            return order.TotalAmount;
        }
    }
}
