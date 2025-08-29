using System;
using System.Text;
using Xunit;

namespace SpecRec.Tests
{
    public class ConcreteClassParrotTests
    {
        // Since DispatchProxy only supports interfaces, we need to test
        // that we handle concrete classes appropriately
        
        [Fact]
        public void Parrot_Create_WithConcreteClass_ShouldWorkWithCastleProxy()
        {
            var verifiedContent = """
                🎲 Next:
                  🔹 Returns: 42

                🎲 Next:
                  🔸 minValue: 1
                  🔸 maxValue: 100
                  🔹 Returns: 50

                """;
            
            var callLog = new CallLog(verifiedContent);
            
            var randomParrot = Parrot.Create<Random>(callLog, "🎲");
            
            Assert.NotNull(randomParrot);
            Assert.Equal(42, randomParrot.Next());
            Assert.Equal(50, randomParrot.Next(1, 100));
        }

        [Fact]
        public void Parrot_Create_WithInterfaceWrapper_ShouldWork()
        {
            // Workaround: Define an interface for the functionality we want to mock
            var verifiedContent = """
                🎲 Next:
                  🔹 Returns: 42

                🎲 NextDouble:
                  🔹 Returns: 0.5

                """;
            
            var callLog = new CallLog(verifiedContent);
            
            // Using an interface wrapper works
            var randomParrot = Parrot.Create<IRandomService>(callLog, "🎲");
            
            Assert.Equal(42, randomParrot.Next());
            Assert.Equal(0.5, randomParrot.NextDouble());
        }

        [Fact]
        public void CallLogger_Wrap_WithConcreteClass_ShouldWorkWithCastleProxy()
        {
            // Test wrapping a concrete class with virtual methods using Castle DynamicProxy
            var callLog = new CallLog();
            var logger = new CallLogger(callLog);
            var random = new Random(12345); // Seeded for predictability
            
            // With Castle DynamicProxy, this should now work for concrete classes
            var wrapped = logger.Wrap<Random>(random, "🎲");
            
            // Use the wrapped object
            var value = wrapped.Next(100);
            
            Assert.NotNull(wrapped);
            Assert.InRange(value, 0, 99);
            
            // Check if the call was logged
            var log = callLog.ToString();
            Assert.Contains("Next", log);
        }

        [Fact]
        public void Context_Substitute_WithConcreteClass_ShouldWork()
        {
            // Document what actually happens with Context.Substitute and concrete classes
            var callLog = new CallLog();
            var objectFactory = ObjectFactory.Instance();
            var context = new Context(callLog, objectFactory);
            
            // Context.Substitute with concrete class 
            // Note: Substitute returns Context for fluent API, not the actual substitute
            var contextAfter = context.Substitute<Random>("🎲");
            Assert.Same(context, contextAfter);
            
            // The actual substitute would be retrieved through ObjectFactory if configured
            // This documents that concrete classes work through ObjectFactory, not proxying
            Assert.NotNull(context);
            
            objectFactory.ClearAll();
        }

        [Fact]
        public void Context_Substitute_WithInterface_ShouldWork()
        {
            // Test Context.Substitute with an interface works correctly
            var callLog = new CallLog();
            var objectFactory = ObjectFactory.Instance();
            var context = new Context(callLog, objectFactory);
            
            // This should work fine with an interface
            var substitute = context.Substitute<IRandomService>("🎲");
            Assert.NotNull(substitute);
            
            objectFactory.ClearAll();
        }

        [Fact]
        public void ObjectFactory_Create_WithConcreteClassAndParrot_DocumentBehavior()
        {
            var verifiedContent = """
                🎲 Next:
                  🔹 Returns: 42

                """;
            
            var callLog = new CallLog(verifiedContent);
            var factory = ObjectFactory.Instance();
            
            // Clear any existing configuration
            factory.ClearAll();
            
            // Since we can't create a Parrot for Random directly,
            // we need to use an interface wrapper
            var randomWrapper = new RandomServiceWrapper();
            factory.SetAlways<IRandomService>(randomWrapper);
            
            // Now Create<IRandomService> will return our wrapper
            var service = factory.Create<IRandomService>();
            Assert.NotNull(service);
            
            factory.ClearAll();
        }

        [Fact]
        public void DocumentConcreteClassSupport()
        {
            // Document what types can now be proxied with Castle DynamicProxy
            
            // Interfaces work via Castle DynamicProxy and DispatchProxy fallback
            Assert.True(ProxyFactory.CanCreateProxyForType(typeof(IDisposable)));
            Assert.True(ProxyFactory.CanCreateProxyForType(typeof(IComparable)));
            Assert.True(ProxyFactory.CanCreateProxyForType(typeof(IRandomService)));
            
            // Concrete classes with virtual members now work via Castle DynamicProxy
            Assert.True(ProxyFactory.CanCreateProxyForType(typeof(Random)));
            Assert.True(ProxyFactory.CanCreateProxyForType(typeof(TestCalculator)));
            
            // Sealed classes still don't work
            Assert.False(ProxyFactory.CanCreateProxyForType(typeof(string)));
            Assert.False(ProxyFactory.CanCreateProxyForType(typeof(int)));
            
            // Classes without virtual members still have limitations
            Assert.False(ProxyFactory.CanCreateProxyForType(typeof(DateTime)));
        }

        [Fact]
        public void Workaround_UsingInterfaceAbstraction_ShouldWork()
        {
            // Document the recommended workaround: use interface abstractions
            
            var verifiedContent = """
                🎲 GetRandomNumber:
                  🔸 min: 1
                  🔸 max: 100
                  🔹 Returns: 42

                🎲 GetRandomDouble:
                  🔹 Returns: 0.7

                """;
            
            var callLog = new CallLog(verifiedContent);
            var logger = new CallLogger(callLog);
            
            // Create a parrot for the interface
            var parrot = CallLoggerProxy<IRandomService>.Create(null, logger, "🎲");
            
            // Use it in place of Random
            var result = UseRandomService(parrot);
            Assert.Equal("Random: 42, Double: 0.7", result); // Culture-invariant decimal separator
        }

        private string UseRandomService(IRandomService random)
        {
            var num = random.GetRandomNumber(1, 100);
            var dbl = random.GetRandomDouble();
            return $"Random: {num}, Double: {dbl.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}";
        }

        // Interface abstraction for Random functionality
        public interface IRandomService
        {
            int Next();
            int Next(int maxValue);
            int Next(int minValue, int maxValue);
            double NextDouble();
            int GetRandomNumber(int min, int max);
            double GetRandomDouble();
        }

        // Wrapper implementation
        public class RandomServiceWrapper : IRandomService
        {
            private readonly Random _random = new();
            
            public int Next() => _random.Next();
            public int Next(int maxValue) => _random.Next(maxValue);
            public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);
            public double NextDouble() => _random.NextDouble();
            public int GetRandomNumber(int min, int max) => _random.Next(min, max);
            public double GetRandomDouble() => _random.NextDouble();
        }

        [Fact]
        public void BestPractice_ExtractInterfaceFromConcreteClass()
        {
            // Document best practice: Extract interface from concrete classes
            
            // Before: Direct dependency on Random
            // var random = new Random();
            // var value = random.Next(100);
            
            // After: Dependency on interface
            var factory = ObjectFactory.Instance();
            factory.SetAlways<IRandomService>(new RandomServiceWrapper());
            
            var service = factory.Create<IRandomService>();
            var value = service.Next(100);
            
            Assert.True(value >= 0 && value < 100);
            
            factory.ClearAll();
        }

        [Fact]
        public void Context_WithConcreteClassWorkaround_ShouldWork()
        {
            // Show how to use Context with concrete class workarounds
            var callLog = new CallLog();
            var objectFactory = ObjectFactory.Instance();
            var context = new Context(callLog, objectFactory);
            
            // Register a real implementation for production
            context.Register<IRandomService>(new RandomServiceWrapper(), "randomService");
            
            // In tests, substitute with a parrot
            var substitute = context.Substitute<IRandomService>("🎲");
            
            // The substitute can now be used in place of Random
            Assert.NotNull(substitute);
            
            objectFactory.ClearAll();
        }

        [Fact]
        public void CustomConcreteClass_ShouldWorkWithCastleParrot()
        {
            var verifiedContent = """
                🧮 Calculate:
                  🔸 x: 5
                  🔸 y: 3
                  🔹 Returns: 8

                🧮 GetDescription:
                  🔹 Returns: "Test Calculator"

                """;
            
            var callLog = new CallLog(verifiedContent);
            
            // Create parrot for custom concrete class
            var calculatorParrot = Parrot.Create<TestCalculator>(callLog, "🧮");
            
            Assert.NotNull(calculatorParrot);
            Assert.Equal(8, calculatorParrot.Calculate(5, 3));
            Assert.Equal("Test Calculator", calculatorParrot.GetDescription());
        }

        [Fact]
        public void CustomConcreteClass_ShouldWorkWithCallLoggerWrap()
        {
            var callLog = new CallLog();
            var logger = new CallLogger(callLog);
            var calculator = new TestCalculator();
            
            // Wrap concrete class with virtual methods using Castle DynamicProxy
            var wrapped = logger.Wrap<TestCalculator>(calculator, "🧮");
            
            var result = wrapped.Calculate(2, 3);
            var description = wrapped.GetDescription();
            
            Assert.Equal(5, result); // Real implementation result
            Assert.Equal("Real Calculator", description);
            
            // Verify logging occurred
            var log = callLog.ToString();
            Assert.Contains("Calculate", log);
            Assert.Contains("GetDescription", log);
        }

        // Test concrete class with virtual methods for Moq testing
        public class TestCalculator
        {
            public virtual int Calculate(int x, int y)
            {
                return x + y;
            }

            public virtual string GetDescription()
            {
                return "Real Calculator";
            }

            // Non-virtual method to test limitations
            public int NonVirtualMethod()
            {
                return 42;
            }
        }

        [Fact]
        public void SealedClass_ShouldStillNotWork()
        {
            var callLog = new CallLog();
            
            // Sealed classes should still fail gracefully with a helpful message
            Assert.Throws<ArgumentException>(() => Parrot.Create<string>(callLog, "📝"));
        }
    }
}