using OrderProcessingSystem.Models;
using OrderProcessingSystem.Repos;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.SyncRepos
{
    public class SyncCouponRepository
    {
        private readonly JsonFileStore _store;

        public SyncCouponRepository(JsonFileStore store)
        {
            _store = store;
        }
        public Coupon? GetByCode(string code)
        {
            Log.Info(nameof(SyncCouponRepository), "open connection...");
            Thread.Sleep(200);
            Log.Info(nameof(SyncCouponRepository), $"Executing query for Coupon #{code}...");
            Thread.Sleep(Random.Shared.Next(300, 601));
            var coupons =  _store.SyncRead<Coupon>(DataFiles.Coupons);



            var coupon = coupons.Find(n => string.Equals(n.Code, code, StringComparison.OrdinalIgnoreCase));
            if (coupon is null)
            {
                Log.Error(nameof(SyncCouponRepository), $"coupon{code} is not found");

                return null;
            }

            Log.Success(
             nameof(SyncCouponRepository),
             $"coupon #{code} found .");
            return coupon;

        }
    }
}
