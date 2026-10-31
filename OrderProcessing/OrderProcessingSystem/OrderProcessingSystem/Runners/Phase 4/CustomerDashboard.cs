using OrderProcessingSystem.Models;

namespace OrderProcessingSystem.Phase_4
{
    public class CustomerDashboard
    {
        public List<EmailMessage> Emails { get; set; } = new();
       public List<PaymentTransaction> Payments { get; set; }
        =new ();
       public List<Order> Orders  { get; set; }
       = new ();
       public Customer? Customer{ get; set; }


    }
}