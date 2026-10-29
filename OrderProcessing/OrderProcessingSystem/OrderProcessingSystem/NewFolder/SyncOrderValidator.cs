using OrderProcessingSystem.Models;
using OrderProcessingSystem.Enums;
using OrderProcessingSystem.Repos;
using OrderProcessingSystem.SyncRepos;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.NewFolder
{
    public class SyncOrderValidator
    {
        private readonly SyncCustomerRepository _customerRepository;
        private readonly SyncProductRepository _productRepository;
        private readonly SyncCouponRepository _couponRepository;
        private readonly SyncOrderRepositpry _orderRepository;

        public SyncOrderValidator(SyncCustomerRepository customerRepository,
                                    SyncProductRepository productRepository,
                                    SyncCouponRepository couponRepository,
                                    SyncOrderRepositpry orderRepository)
        {
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            _couponRepository = couponRepository;
            _orderRepository = orderRepository;
        }



        public void ChangeOrderStatus(Order order, OrderStatus newStatus)
        {
            order.Status = newStatus;
        }

        public void ChangeFailureReson(Order order, string reson)
        {
            order.FailureReason = reson;
        }

      


        public void ValidateOrder(Order order)
        {
            var customer =  _customerRepository.GetById(order.CustomerId);
            if (customer is null)
            {
                ChangeOrderStatus(order, OrderStatus.Rejected);
                ChangeFailureReson(order,$"Customer #{order.CustomerId} not found");
                //SaveOrder(order);
                return;
            }
            if (!customer.IsActive)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = " $Customer is InActive";

                //SaveOrder(order);
                return;

            }

            foreach (var item in order.Items)
            {
                var product =  _productRepository.GetById(item.ProductId);


                if (product is null)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Product #{item.ProductId} not found";

                  //  SaveOrder(order);
                    return;
                }
            }

            foreach (var item in order.Items)
            {
                var product =  _productRepository.GetById(item.ProductId);


                if (product.StockQuantity < item.Quantity)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Insufficient stock for '{product.Name}";

                    //SaveOrder(order);
                    return;
                }
            }

            if (order.CouponCode is not null)
            {
                var coupon =  _couponRepository.GetByCode(order.CouponCode);
                if (coupon is null)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Coupon {order.CouponCode} doesn't Exist";

                   // SaveOrder(order);
                    return;

                }

                if (coupon.ExpiryDate < DateTime.Today)
                {
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Coupon {order.CouponCode} has Expired";

                    //SaveOrder(order);
                    return;
                }
            }


            order.Status = OrderStatus.Validated;
            order.FailureReason = null;
//            SaveOrder(order);






        }

        //public void SaveOrder(Order order)
        //{
        //    _orderRepository.Save(order);
        //}

    }
}
