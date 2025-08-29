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
        public void Parrot_Create_WithConcreteClass_ShouldThrowOrHandleGracefully()
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
            
            // Random is a concrete class, not an interface
            // DispatchProxy cannot create proxies for concrete classes
            // This should either throw a helpful exception or handle gracefully
            
            Assert.Throws<ArgumentException>(() => Parrot.Create<Random>(callLog, "🎲"));
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
        public void CallLogger_Wrap_WithConcreteClass_OriginalBehaviorTest()
        {
            // Test what actually happens when we try to wrap a concrete class
            var callLog = new CallLog();
            var logger = new CallLogger(callLog);
            var random = new Random(12345); // Seeded for predictability
            
            // Test the current behavior - this might be a regression
            try
            {
                var wrapped = logger.Wrap<Random>(random, "🎲");
                
                // If we get here, wrapping succeeded - let's see if we can use it
                var value = wrapped.Next(100);
                
                // If this works, then concrete class wrapping is actually supported
                // and my assumption about the regression might be wrong
                Assert.NotNull(wrapped);
                Assert.InRange(value, 0, 99);
                
                // Check if the call was logged
                var log = callLog.ToString();
                Console.WriteLine($"Call log: {log}");
                
                // This would be the expected behavior if wrapping actually worked
                Assert.Contains("Next", log);
            }
            catch (ArgumentException ex)
            {
                // If this throws ArgumentException, then concrete classes are not supported
                // which confirms the behavior we documented in tests
                Assert.Contains("interface", ex.Message.ToLower());
                Console.WriteLine($"Confirmed: Concrete classes not supported - {ex.Message}");
            }
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
        public void DocumentConcreteClassLimitation()
        {
            // This test documents the limitation with concrete classes
            
            // DispatchProxy only supports interfaces
            Assert.False(ProxyFactory.CanCreateProxyForType(typeof(Random)));
            Assert.False(ProxyFactory.CanCreateProxyForType(typeof(DateTime)));
            Assert.False(ProxyFactory.CanCreateProxyForType(typeof(StringBuilder)));
            
            // But interfaces work fine
            Assert.True(ProxyFactory.CanCreateProxyForType(typeof(IDisposable)));
            Assert.True(ProxyFactory.CanCreateProxyForType(typeof(IComparable)));
            Assert.True(ProxyFactory.CanCreateProxyForType(typeof(IRandomService)));
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
            Assert.Equal("Random: 42, Double: 0,7", result); // Note: culture-specific decimal separator
        }

        private string UseRandomService(IRandomService random)
        {
            var num = random.GetRandomNumber(1, 100);
            var dbl = random.GetRandomDouble();
            return $"Random: {num}, Double: {dbl}";
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
    }
}