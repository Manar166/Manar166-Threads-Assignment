using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessingSystem.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; } // Money is decimal
        public int StockQuantity { get; set; }
        public decimal WeightKg { get; set; }

        public Product() { }
    }
}
