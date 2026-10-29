using OrderProcessingSystem.Models;
using OrderProcessingSystem.Exceptions;
using OrderProcessingSystem.Repos;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace OrderProcessingSystem.Services
{
    public class CustomerService
    {
        private readonly CustomerRepository _customerRepo;

        public CustomerService(CustomerRepository customerRepo)
        {
            _customerRepo = customerRepo;
        }

        public async Task<Customer>GetCustomerAsync(int id,CancellationToken ct)
        {
           var customer= await _customerRepo.GetByIdAsync(id, ct);
            if (customer is null)
            {
                throw new CustomerNotFoundExption(id);
            }
            return customer;

        }
    }
}
