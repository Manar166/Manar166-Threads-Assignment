using OrderProcessingSystem.Models;

namespace OrderProcessingSystem.Repos
{
    public class ProductRepository
    {
        private readonly JsonFileStore _store;

        public ProductRepository(JsonFileStore store)
        {
            _store = store;
        }

        public async Task<Product?> GetByIdAsync(int id, CancellationToken token)
        {
            Log.Info(nameof(ProductRepository), "open connection...");
            await Task.Delay(200, token);
            var products = await _store.ReadAsync<Product>(DataFiles.Products, token);
            Log.Info(nameof(ProductRepository), $"Executing query for product #{id}...");
            await Task.Delay(Random.Shared.Next(300, 601), token);

            var product = products.Find(n => n.Id == id);
            if (product is null)
            {
                Log.Error(nameof(ProductRepository), $"product{id} is not found");
                return null;
            }
            Log.Success(
               nameof(CustomerRepository),
               $"Customer #{id} found ({product.Name}).");
            return product;

        }
        private static readonly SemaphoreSlim semaphoreSlim = new(1, 1);
        public async Task<bool> ReserveStockAsync(int productId, int qty, CancellationToken token)
        {
            if (qty <= 0)
                throw new ArgumentOutOfRangeException(nameof(qty));

            Log.Info(nameof(ProductRepository), "Opening connection...");
            await Task.Delay(200, token);

            Log.Info(nameof(ProductRepository), "Beginning transaction...");
            await Task.Delay(100, token);

            Log.Info(nameof(ProductRepository), $"Updating stock for Product #{productId}...");
            await Task.Delay(400, token);
            bool reserved = false;
            await _store.UpdateAsync<Product>(DataFiles.Products,
                products =>
                {
                    var product = products.Find(n => n.Id == productId);
                    if (product is null || product.StockQuantity < qty)
                    {
                        reserved = false;
                        return products;
                    }
                    product.StockQuantity -= qty;
                    reserved = true;
                    return products;

                }, token);

            if (!reserved)
            {
                Log.Error(nameof(ProductRepository),
                    $"Could not reserve {qty} unit(s) of Product #{productId}.");

                return false;
            }

            Log.Info(nameof(ProductRepository), "Committing transaction...");
            await Task.Delay(200, token);

            Log.Success(nameof(ProductRepository),
                $"Reserved {qty} unit(s) of Product #{productId}.");

            return true;



        }

        public async Task<bool> ReserveStockUnsafeAsync(
                                                         int productId,
                                                         int qty,
                                                         CancellationToken token)
        {
            if (qty <= 0)
                throw new ArgumentOutOfRangeException(nameof(qty));

            
            Log.Info(nameof(ProductRepository), "Opening connection...");
            await Task.Delay(200, token);

            
            var products = await _store.ReadAsync<Product>(
                DataFiles.Products, token);

            var product = products.Find(p => p.Id == productId);

            if (product is null)
            {
                Log.Error(nameof(ProductRepository),
                    $"Product #{productId} not found.");

                return false;
            }

            int currentStock = product.StockQuantity;

            Log.Info(nameof(ProductRepository),
                $"Product #{productId}: stock read = {currentStock}");

            Log.Info(nameof(ProductRepository), "Beginning transaction...");
            await Task.Delay(100, token);

            if (currentStock < qty)
            {
                Log.Error(nameof(ProductRepository),
                    $"Insufficient stock for Product #{productId}.");

                return false;
            }

            int newStock = currentStock - qty;

            Log.Info(nameof(ProductRepository),
                $"Updating Product #{productId} stock...");

            await Task.Delay(400, token);

            product.StockQuantity = newStock;

            await _store.WriteAsync(
                DataFiles.Products,
                products,
                token);

            Log.Success(nameof(ProductRepository),
                $"Reserved {qty} unit(s) of Product #{productId}. " +
                $"New stock = {newStock}");

            return true;
        }

        public async Task<bool> ReleaseStockAsync(int productId, int qty,CancellationToken token)

        {
            if (qty <= 0)
                throw new ArgumentOutOfRangeException(nameof(qty));

            bool released = false;

            await _store.UpdateAsync<Product>(
                DataFiles.Products,
                products =>
                {
                    var product = products.Find(p => p.Id == productId);

                    if (product is null)
                        return products;

                    product.StockQuantity += qty;
                    released = true;

                    return products;
                },
                token);

            return released;
        }


    }
}
