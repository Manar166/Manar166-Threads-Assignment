using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Exceptions
{
    public class CustomerNotFoundExption:Exception
    {
        public CustomerNotFoundExption(int id ):base($"Customer of {id }is not found")
        {
                
        }
    }
}
