using static SpecRec.GlobalObjectFactory;

namespace SpecRec.Tests
{
    public class ParrotExceptionReplayTests
    {
        public interface IPaymentService
        {
            void ProcessPayment(string cardNumber, decimal amount);
            string ChargeCard(string cardNumber, decimal amount);
        }

        public interface IValidationService
        {
            void ValidateInput(string input);
        }

        public interface IDataService
        {
            string GetData(string id);
        }

        public class ValidationException : Exception
        {
            public string ErrorCode { get; set; } = "";
            public List<string> Errors { get; set; } = new();

            public ValidationException(string message) : base(message) { }
            public ValidationException(string message, string errorCode, List<string> errors) : base(message)
            {
                ErrorCode = errorCode;
                Errors = errors;
            }
        }

        public class NotFoundException : Exception
        {
            public NotFoundException(string message) : base(message) { }
        }

        [Theory]
        [SpecRecLogs]
        public async Task Parrot_ShouldThrowExceptionFromVerifiedFile(Context ctx)
        {
            await ctx.Verify(async () =>
            {
                ctx.Substitute<IPaymentService>("💳");
                
                var service = Create<IPaymentService>();
                
                var ex = Assert.Throws<InvalidOperationException>(() => 
                    service.ProcessPayment("invalid-card", 100m));
                
                Assert.Equal("Card validation failed", ex.Message);
            });
        }

        [Theory]
        [SpecRecLogs]
        public async Task Parrot_ShouldThrowExceptionForMethodWithReturnValue(Context ctx)
        {
            await ctx.Verify(async () =>
            {
                ctx.Substitute<IPaymentService>("💳");
                
                var service = Create<IPaymentService>();
                
                var ex = Assert.Throws<InvalidOperationException>(() => 
                    service.ChargeCard("invalid-card", 50m));
                
                Assert.Equal("Card declined", ex.Message);
            });
        }

        [Theory]
        [SpecRecLogs]
        public async Task Parrot_ShouldReplayCustomException(Context ctx)
        {
            await ctx.Verify(async () =>
            {
                ctx.Substitute<IValidationService>("✅");
                
                var service = Create<IValidationService>();
                
                var ex = Assert.Throws<ValidationException>(() => 
                    service.ValidateInput("bad-data"));
                
                Assert.Equal("Input validation failed", ex.Message);
                Assert.Equal("VALIDATION_ERROR", ex.ErrorCode);
                Assert.Equal(3, ex.Errors.Count);
                Assert.Contains("Field1 required", ex.Errors);
                Assert.Contains("Field2 invalid", ex.Errors);
                Assert.Contains("Field3 too long", ex.Errors);
            });
        }

        [Theory]
        [SpecRecLogs]
        public async Task Parrot_ShouldHandleMixedReturnsAndExceptions(Context ctx)
        {
            await ctx.Verify(async () =>
            {
                ctx.Substitute<IDataService>("📊");
                
                var service = Create<IDataService>();
                
                var result1 = service.GetData("valid-id");
                Assert.Equal("data", result1);
                
                Assert.Throws<NotFoundException>(() => 
                    service.GetData("invalid-id"));
                
                var result3 = service.GetData("another-id");
                Assert.Equal("more-data", result3);
            });
        }
    }
}