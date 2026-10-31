using OrderProcessingSystem.Generators;
using OrderProcessingSystem.Interfaces;
using OrderProcessingSystem.Models;
using OrderProcessingSystem.NewFolder;
using OrderProcessingSystem.Phase_4;
using OrderProcessingSystem.Repos;
using OrderProcessingSystem.Runners;
using OrderProcessingSystem.Runners.Phase_4;
using OrderProcessingSystem.Services;
using OrderProcessingSystem.SyncRepos;

namespace OrderProcessingSystem
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };


            CancellationToken ct = cts.Token;

            int indexOfCancelAfter = Array.IndexOf(args, "--cancel-after");
            if (indexOfCancelAfter >= 0)
            {
                if (indexOfCancelAfter + 1 >= args.Length ||
                    !int.TryParse(args[indexOfCancelAfter + 1], out int cancellationTime) ||
                    cancellationTime < 0)
                {
                    Console.WriteLine("Invalid --cancel-after value. Use milliseconds >= 0.");
                    return;
                }
                cts.CancelAfter(cancellationTime);
                Console.WriteLine($"Cancellation scheduled after {cancellationTime} ms.");
            }

            int phase;
            int phaseIndex = Array.IndexOf(args, "--phase");
            if (phaseIndex >= 0)
            {
                if (phaseIndex + 1 >= args.Length ||
                    !int.TryParse(args[phaseIndex + 1], out phase) ||
                    phase < 1 || phase > 4)
                {
                    Console.WriteLine("Use --phase 1, 2, 3, or 4.");
                    return;
                }
            }
            else
            {
                Console.WriteLine("=== Order Processing System ===");
                Console.WriteLine("1 - Phase 1: Sync / Async validation");
                Console.WriteLine("2 - Phase 2: Pricing and concurrency");
                Console.WriteLine("3 - Phase 3: Checkout, payment, email");
                Console.WriteLine("4 - Phase 4: Live progress, dashboard, shipping, fraud / email");
                Console.Write("Select phase (1-4): ");
                if (!int.TryParse(Console.ReadLine(), out phase) || phase < 1 || phase > 4)
                {
                    Console.WriteLine("Invalid phase selection.");
                    return;
                }
            }
            await RunPhaseAsync(phase, cts.Token);
        }



        public static async Task RunPhaseAsync(int phase, CancellationToken ct)
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

                var pricingService = new PricingService(asyncProductRepository, asyncCustomerRepository, asyncCouponRepository, new List<int> { 7 });
                var orderPricer = new OrderPricer(pricingService);
                var orderValidator = new OrderValidator(asyncCustomerRepository, asyncProductRepository, asyncCouponRepository, asyncOrderRepository);
                var invoiceGenerator = new InvoiceGenerator(fileStore);
                var orderProcessorV2 = new OrderProcessorV2(asyncProductRepository, orderPricer, asyncOrderRepository, invoiceGenerator);
                RaceConditionDemo raceConditionDemo = new RaceConditionDemo(fileStore, asyncProductRepository);
                ReportGenerator reportGenerator = new ReportGenerator(asyncProductRepository, asyncOrderRepository, fileStore);
                Phase2Runner phase2Runner = new Phase2Runner(incomingOrders, orderPricer, dataSeeder, raceConditionDemo, orderValidator, orderProcessorV2, asyncOrderRepository, reportGenerator);


                CustomerService customerService = new CustomerService(asyncCustomerRepository);
                var inventoryService = new InventoryService(asyncProductRepository);
                IPaymentGateway paymentGateway = new PaymentGatway(
                    fileStore,
                    timeoutOrderIds: new HashSet<int> { 5 },
                    declineOrderIds: new HashSet<int> { 8 }
                );

                IEmailService emailService = new EmailService(fileStore, new Dictionary<int, int>
                {
                    [3] = 2,
                    [17] = int.MaxValue

                });

                OrderProcessor orderProcessor = new OrderProcessor(customerService, inventoryService, pricingService, asyncOrderRepository, paymentGateway, emailService);
                Phase3Runner phase3Runner = new Phase3Runner(dataSeeder, asyncOrderRepository, orderProcessor, fileStore);
                Func<Order, CancellationToken, Task<decimal>> shippingFormula = (order, token) =>
                                                                               pricingService.CalculateShippingCostAsync(order, token);




                IShippingCarrier[] carriers =
                                            {
                                new FastShip(shippingFormula),
                                new CairoExpress(shippingFormula),
                                new NileCourier(shippingFormula)
                                            };



                var shippingQuoteService = new ShippingQuoteService(carriers);
                IShippingCarrier[] failingCarriers =
                                                    {
                                                        new FastShip(shippingFormula, new[] { 6 }),
                                                        new CairoExpress(shippingFormula, new[] { 6 }),
                                                        new NileCourier(shippingFormula, new[] { 6 })
                                                    };

                var failingShippingService =
                    new ShippingQuoteService(failingCarriers);

                var paymentRepository = new PaymentRepository(fileStore, asyncOrderRepository);
                var emailRepository = new EmailRepository(fileStore, asyncOrderRepository);

                var dashboardService = new CustomerDashboardService(
                            asyncCustomerRepository,
                            asyncOrderRepository,
                            paymentRepository,
                            emailRepository,
                            failPaymentsForCustomerIds: new[] { 99 });
                Phase4Runner phase4Runner = new Phase4Runner(
                                                            dataSeeder,
                                                            orderProcessor,
                                                            asyncOrderRepository,
                                                            fileStore, dashboardService, shippingQuoteService, failingShippingService);

                IEmailService emailServiceNoFailures = new EmailService(
                                                                        fileStore,
                                                                        new Dictionary<int, int>());

                var task104Runner = new Task104Runner(emailServiceNoFailures);

                switch (phase)
                {
                    case 1:
                        await phase1Runner.RunAsync(ct);
                        break;
                    case 2:
                        await phase2Runner.RunAsync(ct);
                        break;
                    case 3:
                        await phase3Runner.RunAsync(ct);
                        break;
                    case 4:
                        // Phase4Runner resets Data. Run Task104 afterwards.
                        await phase4Runner.RunAsync(ct);
                        await task104Runner.RunAsync(ct);
                        break;
                }



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
