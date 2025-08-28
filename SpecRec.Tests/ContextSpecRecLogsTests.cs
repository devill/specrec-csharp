using Xunit;
using static SpecRec.GlobalObjectFactory;

namespace SpecRec.Tests
{
    public class ContextSpecRecLogsTests
    {
        [Theory]
        [SpecRecLogs]
        public async Task BasicSubstitutePattern(Context ctx, int passengerCount, string airlineCode = "UA")
        {
            await ctx.Verify(async () =>
            {
                ctx.Substitute<IBookingRepository>("💾")
                   .Substitute<IFlightService>("✈️");

                var coordinator = new BookingCoordinator();
                return coordinator.BookFlight(passengerCount, airlineCode);
            });
        }

        [Theory]
        [SpecRecLogs]
        public async Task ObjectRegistrationPattern(Context ctx, decimal amount, string currency = "USD")
        {
            var paymentProcessor = new PaymentProcessorStub();
            var logger = new FakeLogger();
            
            ctx.SetAlways<IPaymentProcessor>(paymentProcessor, "mainProcessor")
               .SetOne<ILogger>(logger, "logger1")
               .SetOne<ILogger>(new FakeLogger(), "logger2");

            var service = new PaymentService();
            var result = service.ProcessPayment(amount, currency);
            
            ctx.CallLog.AppendLine($"Returns: {result}");
            await ctx.CallLog.Verify();
        }

        [Theory]
        [SpecRecLogs]
        public async Task CallLoggerWrappingPattern(Context ctx, string endpoint, int retryCount = 3)
        {
            var apiClient = new HttpApiClientStub();
            var trackedClient = ctx.Wrap<IHttpApiClient>(apiClient, "🔗");

            var service = new ExternalService(trackedClient);
            var result = service.FetchDataWithRetries(endpoint, retryCount);
            
            ctx.CallLog.AppendLine($"Returns: {result}");
            await ctx.CallLog.Verify();
        }

        [Theory]
        [SpecRecLogs]
        public async Task ParrotCreationPattern(Context ctx, string input, bool strictMode = false)
        {
            var validator = ctx.Parrot<IValidator>("✅");
            
            var service = new ValidationService(validator);
            var result = service.ValidateUserInput(input, strictMode);
            
            ctx.CallLog.AppendLine($"Returns: {result}");
            await ctx.CallLog.Verify();
        }

        [Theory]
        [SpecRecLogs]
        public async Task FluentChainingPattern(Context ctx, string userName, bool isAdmin = false)
        {
            var userService = new UserServiceStub();
            var logger = new FakeLogger();

            try
            {
                ctx.Substitute<IAuthService>("🔐")
                    .SetAlways<IUserService>(userService, "userSvc")
                    .SetOne<ILogger>(logger, "mainLogger");

                var coordinator = new UserCoordinator();
                var result = coordinator.RegisterUser(userName, isAdmin);
                ctx.CallLog.AppendLine($"Returns: {result}");
            }
            finally
            {
                await ctx.CallLog.Verify();    
            }
        }

        [Theory]
        [SpecRecLogs]
        public async Task ComplexIntegrationPattern(Context ctx, string orderType, int quantity = 1)
        {
            var inventoryService = new InventoryServiceStub();
            var priceCalculator = new PriceCalculatorStub();

            try
            {
                ctx.Substitute<IOrderValidator>("📋")
                    .Substitute<IPaymentGateway>("💳")
                    .SetAlways<IInventoryService>(inventoryService, "inventory")
                    .SetOne<IPriceCalculator>(priceCalculator, "calculator");

                var orderProcessor = new OrderProcessor();
                var result = orderProcessor.ProcessOrder(orderType, quantity);

                ctx.CallLog.AppendLine($"Returns: {result}");
            }
            finally
            {
                await ctx.CallLog.Verify();                
            }
        }

        // Test to verify Context.ToString() provides clean test names
        [Theory]
        [SpecRecLogs]
        public async Task ContextDisplayName(Context ctx, string testScenario = "default")
        {
            ctx.CallLog.AppendLine($"Test scenario: {testScenario}");
            ctx.CallLog.AppendLine($"Context display name: {ctx}");
            await ctx.CallLog.Verify();
        }
        
        // Test to verify Context.ToString() provides clean test names
        [Theory]
        [SpecRecLogs]
        public async Task VerifyContext(Context ctx, string testScenario = "default")
        {
            await ctx.Verify(async () =>
            {
                ctx.CallLog.AppendLine($"Test scenario: {testScenario}");
                ctx.CallLog.AppendLine($"Context display name: {ctx}");
                await Task.CompletedTask;
            });
        }
    }

    // Test interfaces and stub implementations for approval tests
    public interface IBookingRepository
    {
        int CreateReservation(string airlineCode, int passengerCount);
        bool IsAvailable(string airlineCode, int passengerCount);
    }

    public interface IFlightService
    {
        string GetFlightInfo(string airlineCode);
        decimal CalculatePrice(string airlineCode, int passengerCount);
    }

    public interface IPaymentProcessor
    {
        string ProcessPayment(decimal amount, string currency);
    }

    public interface ILogger
    {
        void Log(string message);
    }

    public interface IHttpApiClient
    {
        string Get(string endpoint);
    }

    public interface IValidator
    {
        bool Validate(string input, bool strictMode);
        string[] GetErrors();
    }

    public interface IAuthService
    {
        bool Authenticate(string userName);
        string[] GetUserRoles(string userName);
    }

    public interface IUserService
    {
        int CreateUser(string userName, bool isAdmin);
    }

    public interface IOrderValidator
    {
        bool ValidateOrder(string orderType, int quantity);
    }

    public interface IPaymentGateway
    {
        string ProcessPayment(decimal amount);
    }

    public interface IInventoryService
    {
        bool CheckAvailability(string item, int quantity);
        void ReserveItems(string item, int quantity);
    }

    public interface IPriceCalculator
    {
        decimal CalculatePrice(string item, int quantity);
    }

    // Service implementations that use the dependencies
    public class BookingCoordinator
    {
        public string BookFlight(int passengerCount, string airlineCode)
        {
            var repository = Create<IBookingRepository>();
            var flightService = Create<IFlightService>();
            
            if (!repository.IsAvailable(airlineCode, passengerCount))
            {
                return "No availability";
            }
            
            var price = flightService.CalculatePrice(airlineCode, passengerCount);
            var reservationId = repository.CreateReservation(airlineCode, passengerCount);
            
            return $"Booked flight {airlineCode} for {passengerCount} passengers, reservation #{reservationId}, price: {price:C}";
        }
    }

    public class PaymentService
    {
        public string ProcessPayment(decimal amount, string currency)
        {
            var processor = Create<IPaymentProcessor>();
            var logger = Create<ILogger>();
            
            logger.Log($"Processing payment of {amount} {currency}");
            var result = processor.ProcessPayment(amount, currency);
            logger.Log($"Payment result: {result}");
            
            return result;
        }
    }

    public class ExternalService
    {
        private readonly IHttpApiClient _client;

        public ExternalService(IHttpApiClient client)
        {
            _client = client;
        }

        public string FetchDataWithRetries(string endpoint, int retryCount)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    var data = _client.Get(endpoint);
                    if (!string.IsNullOrEmpty(data))
                    {
                        return data;
                    }
                }
                catch
                {
                    // Continue to next retry
                }
            }
            
            return "Failed after retries";
        }
    }

    public class ValidationService
    {
        private readonly IValidator _validator;

        public ValidationService(IValidator validator)
        {
            _validator = validator;
        }

        public string ValidateUserInput(string input, bool strictMode)
        {
            var isValid = _validator.Validate(input, strictMode);
            if (isValid)
            {
                return "Valid";
            }
            
            var errors = _validator.GetErrors();
            return $"Invalid: {string.Join(", ", errors)}";
        }
    }

    public class UserCoordinator
    {
        public string RegisterUser(string userName, bool isAdmin)
        {
            var authService = Create<IAuthService>();
            var userService = Create<IUserService>();
            var logger = Create<ILogger>();
            
            if (!authService.Authenticate(userName))
            {
                logger.Log($"Authentication failed for {userName}");
                return "Authentication failed";
            }
            
            var userId = userService.CreateUser(userName, isAdmin);
            var roles = authService.GetUserRoles(userName);
            
            logger.Log($"User {userName} registered with ID {userId} and roles: {string.Join(", ", roles)}");
            return $"User registered with ID {userId}";
        }
    }

    public class OrderProcessor
    {
        public string ProcessOrder(string orderType, int quantity)
        {
            var validator = Create<IOrderValidator>();
            var gateway = Create<IPaymentGateway>();
            var inventory = Create<IInventoryService>();
            var calculator = Create<IPriceCalculator>();
            
            if (!validator.ValidateOrder(orderType, quantity))
            {
                return "Invalid order";
            }
            
            if (!inventory.CheckAvailability(orderType, quantity))
            {
                return "Out of stock";
            }
            
            var price = calculator.CalculatePrice(orderType, quantity);
            var paymentResult = gateway.ProcessPayment(price);
            
            if (paymentResult.StartsWith("Success"))
            {
                inventory.ReserveItems(orderType, quantity);
                return $"Order processed: {quantity}x {orderType} for {price:C}";
            }
            
            return "Payment failed";
        }
    }

    // Stub implementations for testing
    public class PaymentProcessorStub : IPaymentProcessor
    {
        public string ProcessPayment(decimal amount, string currency) => "Processing stubbed";
    }

    public class FakeLogger : ILogger
    {
        public void Log(string message) { /* Fake implementation */ }
    }

    public class HttpApiClientStub : IHttpApiClient
    {
        public string Get(string endpoint) => "API response data";
    }

    public class UserServiceStub : IUserService
    {
        public int CreateUser(string userName, bool isAdmin) => 42;
    }

    public class InventoryServiceStub : IInventoryService
    {
        public bool CheckAvailability(string item, int quantity) => true;
        public void ReserveItems(string item, int quantity) { }
    }

    public class PriceCalculatorStub : IPriceCalculator
    {
        public decimal CalculatePrice(string item, int quantity) => 99.99m;
    }
}