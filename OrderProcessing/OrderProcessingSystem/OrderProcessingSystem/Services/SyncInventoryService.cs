
using OrderProcessingSystem.Models;
using OrderProcessingSystem.SyncRepos;

namespace OrderProcessingSystem.Services
{
    public class SyncInventoryService
    {
        private readonly SyncProductRepository productRepository;

        public SyncInventoryService(
            SyncProductRepository productRepository)
        {
            this.productRepository = productRepository;
        }

        public bool ReserveStock(Order order)
        {
            List<OrderItem> reservedItems = new();

            foreach (var item in order.Items)
            {
                bool reserved = productRepository.ReserveStock(
                    item.ProductId,
                    item.Quantity);

                if (reserved)
                {
                    reservedItems.Add(item);
                }
                else
                {
                    // Roll back previously reserved items.
                    foreach (var reservedItem in reservedItems)
                    {
                        bool released = productRepository.ReleaseStock(
                            reservedItem.ProductId,
                            reservedItem.Quantity);

                        if (!released)
                        {
                            throw new InvalidOperationException(
                                $"Failed to roll back Product #{reservedItem.ProductId}");
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        public bool ReleaseStock(Order order)
        {
            bool allReleased = true;

            foreach (var item in order.Items)
            {
                bool released = productRepository.ReleaseStock(
                    item.ProductId,
                    item.Quantity);

                if (!released)
                {
                    allReleased = false;

                    Console.WriteLine(
                        $"Failed to release Product #{item.ProductId}");
                }
            }

            return allReleased;
        }
    }
}
