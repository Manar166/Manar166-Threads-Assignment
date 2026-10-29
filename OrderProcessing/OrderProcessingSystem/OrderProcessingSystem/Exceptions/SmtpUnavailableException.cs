using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Exceptions
{
    public class SmtpUnavailableException:Exception
    {
        public SmtpUnavailableException(string message):base(message)
        {
            
        }
    }
}
