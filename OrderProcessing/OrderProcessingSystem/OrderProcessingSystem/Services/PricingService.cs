using OrderProcessingSystem.Classes;
using OrderProcessingSystem.Enums;
using OrderProcessingSystem.Exceptions;
using OrderProcessingSystem.Repos;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Services
{
    public class PricingService
    {
        private readonly ProductRepository _productRepo;
        private readonly CustomerRepository _customertRepo;
        private readonly CouponRepository _couponRepo;
        List<int> _failShippingForOrderIds;


        public PricingService(ProductRepository productRepo, CustomerRepository customertRepo, CouponRepository couponRepo, List<int> failShippingForOrderIds)
        {
            _productRepo = productRepo;
            _customertRepo = customertRepo;
            _couponRepo = couponRepo;
            _failShippingForOrderIds = failShippingForOrderIds;
        }

        public async Task<decimal> CalculateSubTotalAsync(Order order,CancellationToken ct)
        {
            decimal subTotal = 0m;

            foreach (var item in order.Items)
            {
                int itemId = item.ProductId;
                var product = await _productRepo.GetByIdAsync(itemId, ct);
                if (product == null)
                { return 0.0m; }

                item.UnitPrice = Math.Round( product.Price,2, MidpointRounding.AwayFromZero); 



                subTotal += product.Price * item.Quantity;

                    
                   
            }
            order.SubTotal = Math.Round(subTotal, 2, MidpointRounding.AwayFromZero);

            return order.SubTotal;

        }

        public async Task<decimal> GetDiscountAsync(Order order ,CancellationToken ct)
        {
            decimal coupnDiscount = 0m;
            decimal customerLoalityDiscount = 0m;
            decimal totalDiscount = 0m;
            var customer =await _customertRepo.GetByIdAsync(order.CustomerId, ct);
            if (customer == null)
            {
                throw new InvalidOperationException($"Customer #{order.CustomerId} not found");

            }

            customerLoalityDiscount = customer.LoyaltyTier switch
            {
                LoyaltyTier.Bronze => 0m,
                LoyaltyTier.Silver => 5m,
                LoyaltyTier.Gold => 10m,
                _=>0m

            };

            if (!String.IsNullOrWhiteSpace(order.CouponCode))
            {


                var coupon = await _couponRepo.GetByCodeAsync(order.CouponCode, ct);

                if (coupon is null)
                {
                    throw new InvalidOperationException(
                        $"Coupon '{order.CouponCode}' does not exist");
                }

                if (coupon.ExpiryDate < DateTime.UtcNow)
                {
                    throw new InvalidOperationException(
                        $"Coupon '{order.CouponCode}' has expired");
                }
                coupnDiscount = coupon.DiscountPercent;




            }


            decimal discountPercentage = Math.Min(customerLoalityDiscount + coupnDiscount, 30m);
            decimal discount = Math.Round(order.SubTotal * discountPercentage / 100m,
                                            2,
                                            MidpointRounding.AwayFromZero);
            return discount;

        }


        public async Task<decimal> CalculateShippingCostAsync(Order order,CancellationToken ct)
        {
            if (_failShippingForOrderIds.Contains(order.Id))
            {
                throw new ShippingProviderUnavailableException("Shipping provider unavailable");
            }
            
            if (order.SubTotal >= 1000)
            {
                return 0.0m;
            }

            decimal totalWeight=0m;
            decimal ShippingCost=0m;
            foreach (var item in order.Items)
            {
                int productId = item.ProductId;
                var product=  await _productRepo.GetByIdAsync(productId, ct);
                if (product == null)
                {
                    throw new NullReferenceException("product is null");
                }
                totalWeight += product.WeightKg * item.Quantity;
            }
            int totalQuantity = order.Items.Sum(item => item.Quantity);

            ShippingCost = 5.00m + 1.50m * totalQuantity + 2.00m * totalWeight;
           return Math.Round(ShippingCost, 2, MidpointRounding.AwayFromZero);

        }

    }
}
