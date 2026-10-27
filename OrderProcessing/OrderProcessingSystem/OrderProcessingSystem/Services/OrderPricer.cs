using OrderProcessingSystem.Classes;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Services
{
    public class OrderPricer
    {
        private readonly PricingService _pricingService;

        public OrderPricer(PricingService pricingService)
        {
            _pricingService = pricingService;
        }

        public async Task PriceOrderAsync(Order order, CancellationToken ct)
        {
          decimal subToal= await _pricingService.CalculateSubTotalAsync(order, ct);

          Task<decimal> discount= _pricingService.GetDiscountAsync(order, ct);
          Task<decimal> shippingCost = _pricingService.CalculateShippingCostAsync(order, ct);
          await Task.WhenAll(discount, shippingCost);

            order.Discount = await discount;
            order.ShippingCost = await shippingCost;

            order.TotalAmount = Math.Round((order.SubTotal - order.Discount + order.ShippingCost), 2, MidpointRounding.AwayFromZero);





        }
        public async Task PriceOrderSequentialAsync(Order order, CancellationToken ct)
        {
            decimal subToal = await _pricingService.CalculateSubTotalAsync(order, ct);

            decimal discount =await _pricingService.GetDiscountAsync(order, ct);
            decimal shippingCost =await _pricingService.CalculateShippingCostAsync(order, ct);
           

            order.Discount =  discount;
            order.ShippingCost =  shippingCost;

            order.TotalAmount = Math.Round((order.SubTotal - order.Discount + order.ShippingCost), 2, MidpointRounding.AwayFromZero);





        }
    }
}
