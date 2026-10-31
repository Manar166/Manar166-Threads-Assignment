using OrderProcessingSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.SyncRepos
{
    public class SyncCustomerRepository
    {

        private readonly JsonFileStore _fileStore;

        public SyncCustomerRepository(JsonFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        public Customer GetById(int id)
        {
            Log.Info(nameof(SyncCustomerRepository), "openConnection ...");
            Thread.Sleep(200);
            Log.Info(nameof(SyncCustomerRepository), $"Executing query for Customer #{id}...");

            Thread.Sleep(Random.Shared.Next(300, 601));
            var customers = _fileStore.SyncRead<Customer>(DataFiles.Customers);

            var customer= customers.ToList().Find(n => n.Id == id);

            if (customer is null)
            {
                Log.Error(nameof(SyncCustomerRepository), $"customer{id} is not found");
                return null;
            }

            Log.Success(
           nameof(SyncCustomerRepository),
           $"Customer #{id} found ({customer.Name}).");
            return customer;


        }
    }
}



