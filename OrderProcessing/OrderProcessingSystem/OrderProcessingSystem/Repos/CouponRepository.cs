using OrderProcessingSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Repos
{
    public class CouponRepository
    {
        private readonly JsonFileStore _store;

        public CouponRepository(JsonFileStore store)
        {
            _store = store;
        }
        public async Task<Coupon?> GetByCodeAsync(string code,CancellationToken token)
        {
            Log.Info(nameof(CouponRepository), "open connection...");
            await Task.Delay(200, token); 
            Log.Info(nameof(CouponRepository), $"Executing query for Coupon #{code}...");
            await Task.Delay(Random.Shared.Next(300, 601), token);
            var coupons = await _store.ReadAsync<Coupon>(DataFiles.Coupons, token);
          


            var coupon = coupons.Find(n => string.Equals(n.Code, code, StringComparison.OrdinalIgnoreCase));
            if (coupon is null)
            {
                Log.Error(nameof(CouponRepository), $"coupon{code} is not found");

                return null;
            }

            Log.Success(
             nameof(CouponRepository),
             $"coupon #{code} found .");
            return coupon;

        }

    }
}
