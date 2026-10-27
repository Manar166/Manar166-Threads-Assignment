using OrderProcessingSystem.Generators;
using OrderProcessingSystem.NewFolder;
using OrderProcessingSystem.Repos;
using OrderProcessingSystem.Runners;
using OrderProcessingSystem.Services;
using OrderProcessingSystem.SyncRepos;
using System.Threading.Channels;

namespace OrderProcessingSystem
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };


            CancellationToken ct = cts.Token;

           int indexOfCancelAfter= Array.IndexOf(args, "cancel-after");
            int cancellationTime;
            if (indexOfCancelAfter > 0)
            {
                if (indexOfCancelAfter + 1 >= args.Length || !int.TryParse(args[indexOfCancelAfter + 1], out  cancellationTime) || cancellationTime <0)
                {

                    Console.WriteLine("Invalid value for --cancel-after. Provide a non-negative number of milliseconds.");
                    return;
                }

                cts.CancelAfter(cancellationTime);

                Console.WriteLine($"Cancellation scheduled after {cancellationTime} ms.");
            }

            await Phase1(cts.Token);
        }


        public static async Task Phase1(CancellationToken ct)
        {

            try
            {
                var fileStore = new JsonFileStore();

                var dataSeeder = new DataSeeder(fileStore);
                var incomingOrderRepository = new IncomingOrderRepository(fileStore);


                var syncOrderRepository = new SyncOrderRepositpry(fileStore);
                var syncCustomerRepository = new SyncCustomerRepository(fileStore);
                var syncProductRepository = new SyncProductRepository(fileStore);
                var syncCouponRepository = new SyncCouponRepository(fileStore);

                SyncOrderValidator syncOrderValidator = new SyncOrderValidator(syncCustomerRepository, syncProductRepository, syncCouponRepository, syncOrderRepository);

                var asyncOrderRepository = new OrderRepository(fileStore);
                var asyncCustomerRepository = new CustomerRepository(fileStore);
                var asyncProductRepository = new ProductRepository(fileStore);
                var asyncCouponRepository = new CouponRepository(fileStore);
                OrderValidator asyncOrderValidator = new OrderValidator(asyncCustomerRepository, asyncProductRepository, asyncCouponRepository, asyncOrderRepository);

                Phase1Runner phase1Runner = new Phase1Runner(syncOrderValidator, asyncOrderValidator, asyncOrderRepository, fileStore);

                var incomingOrders = new IncomingOrderRepository(fileStore);

                var pricingService = new PricingService(asyncProductRepository, asyncCustomerRepository, asyncCouponRepository, new List<int> {7});
                var orderPricer = new OrderPricer(pricingService);
                var orderValidator = new OrderValidator(asyncCustomerRepository, asyncProductRepository, asyncCouponRepository,asyncOrderRepository);
                var invoiceGenerator = new InvoiceGenerator(fileStore);
                var orderProcessorV2 = new OrderProcessorV2(asyncProductRepository, orderPricer, asyncOrderRepository, invoiceGenerator);
                RaceConditionDemo raceConditionDemo = new RaceConditionDemo(fileStore, asyncProductRepository);
                ReportGenerator reportGenerator = new ReportGenerator(asyncProductRepository, asyncOrderRepository, fileStore);
                Phase2Runner phase2Runner = new Phase2Runner(incomingOrders, orderPricer,dataSeeder,raceConditionDemo,orderValidator,orderProcessorV2, asyncOrderRepository, reportGenerator);



                // await phase1Runner.RunAsync(ct);
                await phase2Runner.RunAsync(ct);

                //await raceConditionDemo.RunAsync();


            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation was cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }


        }
    }
}
