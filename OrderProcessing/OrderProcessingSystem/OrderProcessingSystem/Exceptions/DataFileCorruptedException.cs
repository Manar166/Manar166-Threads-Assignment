using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OrderProcessingSystem.Exceptions
{
    public class DataFileCorruptedException:Exception
    {

   

        public DataFileCorruptedException(string fileName, Exception innerException)
            : base($"The JSON file '{fileName}' is corrupted or contains invalid JSON.",
                innerException)
        {
        }

    }
}
