using OrderProcessingSystem.Models;
using OrderProcessingSystem.Repos;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.SyncRepos
{
    public class SyncProductRepository
    {
        private readonly JsonFileStore _store;

        public SyncProductRepository(JsonFileStore store)
        {
            _store = store;
        }

        public  Product? GetById(int id)
        {
            Log.Info(nameof(SyncProductRepository), "open connection...");
            Thread.Sleep(200);
            var products =  _store.SyncRead<Product>(DataFiles.Products);
            Log.Info(nameof(SyncProductRepository), $"Executing query for product #{id}...");
            Thread.Sleep(Random.Shared.Next(300, 601));
            
            var product = products.Find(n => n.Id == id);
            if (product is null)
            {
                Log.Error(nameof(SyncProductRepository), $"product{id} is not found");
                return null;
            }
            Log.Success(
               nameof(SyncProductRepository),
               $"product #{id} found ({product.Name}).");
            return product;

        }
        public bool ReserveStock(int productId, int qty)
        {
            if (qty <= 0)
                throw new ArgumentOutOfRangeException(nameof(qty));

            Log.Info(nameof(SyncProductRepository), "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(SyncProductRepository), "Beginning transaction...");
            Thread.Sleep(100);

            Log.Info(nameof(SyncProductRepository), $"Updating stock for Product #{productId}...");
            Thread.Sleep(400);
            bool reserved = false;
             _store.Syncupdate<Product>(DataFiles.Products,
                products =>
                {
                    var product = products.Find(n => n.Id == productId);
                    if (product is null || product.StockQuantity <qty)
                    {
                        reserved = false;
                        return products;
                    }
                    product.StockQuantity -= qty;
                    reserved = true;
                    return products;

                });

            if (!reserved)
            {
                Log.Error(nameof(SyncProductRepository),
                    $"Could not reserve {qty} unit(s) of Product #{productId}.");

                return false;
            }

            Log.Info(nameof(SyncProductRepository), "Committing transaction...");
            Thread.Sleep(200);
                       Log.Success(nameof(SyncProductRepository),
                $"Reserved {qty} unit(s) of Product #{productId}.");

            return true;



        }


        public bool ReleaseStock(int productId, int qty)
        {
            if (qty <= 0)
                throw new ArgumentOutOfRangeException(nameof(qty));

            Log.Info(nameof(SyncProductRepository),
                "Opening connection...");
            Thread.Sleep(200);

            Log.Info(nameof(SyncProductRepository),
                "Beginning release transaction...");
            Thread.Sleep(100);

            Log.Info(nameof(SyncProductRepository),
                $"Restoring stock for Product #{productId}...");
            Thread.Sleep(400);

            bool released = false;

            _store.Syncupdate<Product>(
                DataFiles.Products,
                products =>
                {
                    Product? product = products.Find(
                        p => p.Id == productId);

                    if (product is null)
                    {
                        released = false;
                        return products;
                    }

                    product.StockQuantity += qty;
                    released = true;

                    return products;
                });

            if (!released)
            {
                Log.Error(nameof(SyncProductRepository),
                    $"Could not release {qty} unit(s) of Product #{productId}.");

                return false;
            }

            Log.Info(nameof(SyncProductRepository),
                "Committing release transaction...");
            Thread.Sleep(200);

            Log.Success(nameof(SyncProductRepository),
                $"Released {qty} unit(s) of Product #{productId}.");

            return true;
        }

    }
}
