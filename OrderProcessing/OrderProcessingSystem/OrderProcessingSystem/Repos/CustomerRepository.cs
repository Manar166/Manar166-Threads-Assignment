using OrderProcessingSystem.Classes;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Repos
{
    public class CustomerRepository
    {
        private readonly JsonFileStore _fileStore;

        public CustomerRepository(JsonFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        public async Task<Customer?> GetByIdAsync(int id,CancellationToken token)
        {
            Log.Info(nameof(CustomerRepository), "openConnection ...");
            await Task.Delay(200, token);
            Log.Info(nameof(CustomerRepository), $"Executing query for Customer #{id}...");

            await Task.Delay(Random.Shared.Next(300, 601), token);
            var customers=await _fileStore.ReadAsync<Customer>(DataFiles.Customers, token);

            var customer= customers.ToList().Find(n => n.Id == id);

            if (customer is null)
            {
                Log.Error(nameof(CustomerRepository), $"customer{id} is not found");
                return null;
            }

            Log.Success(
           nameof(CustomerRepository),
           $"Customer #{id} found ({customer.Name}).");
            return customer;


        }
    }
}
