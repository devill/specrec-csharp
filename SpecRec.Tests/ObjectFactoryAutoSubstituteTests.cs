namespace SpecRec.Tests;

public class ObjectFactoryAutoSubstituteTests
{
    public class SetAutoParrotTests
    {
        [Fact]
        public void SetAutoParrot_WithValidTypeAndCallLog_ShouldConfigureAutoSubstitute()
        {
            var factory = new ObjectFactory();
            var callLog = new CallLog();
            
            factory.SetAutoParrot<IAutoTestService>(callLog, "🔧");
            
            // Should now have auto-substitute configured
            Assert.True(factory.HasAutoParrot<IAutoTestService>());
        }

        [Fact] 
        public void SetAutoParrot_CalledTwice_ShouldOverrideConfiguration()
        {
            var factory = new ObjectFactory();
            var callLog1 = new CallLog();
            var callLog2 = new CallLog();
            
            factory.SetAutoParrot<IAutoTestService>(callLog1, "🔧");
            factory.SetAutoParrot<IAutoTestService>(callLog2, "⚡");
            
            // Should still be configured (latest config wins)
            Assert.True(factory.HasAutoParrot<IAutoTestService>());
        }

        [Fact]
        public void HasAutoSubstitute_WithoutConfiguration_ShouldReturnFalse()
        {
            var factory = new ObjectFactory();
            
            Assert.False(factory.HasAutoParrot<IAutoTestService>());
        }
    }

    public class CreateWithAutoSubstituteTests
    {
        [Fact]
        public void Create_WithAutoSubstituteConfigured_ShouldReturnParrotInstance()
        {
            var factory = new ObjectFactory();
            var callLog = new CallLog();
            
            factory.SetAutoParrot<IAutoTestService>(callLog, "🔧");
            
            var result = factory.Create<IAutoTestService>();
            
            // Should return a parrot proxy, not the actual implementation
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IAutoTestService>(result);
            // The actual type should be a proxy type
            Assert.NotEqual(typeof(AutoTestServiceImpl), result.GetType());
        }

        [Fact]
        public void Create_WithoutAutoSubstitute_ShouldCreateNormalInstance()
        {
            var factory = new ObjectFactory();
            
            // This should fail because there's no concrete implementation registered and no auto-substitute
            Assert.Throws<MissingMethodException>(() => factory.Create<IAutoTestService>());
        }

        [Fact]
        public void Create_WithAutoSubstituteThenClear_ShouldReturnToNormalBehavior()
        {
            var factory = new ObjectFactory();
            var callLog = new CallLog();
            
            factory.SetAutoParrot<IAutoTestService>(callLog, "🔧");
            factory.ClearAutoParrot<IAutoTestService>();
            
            Assert.False(factory.HasAutoParrot<IAutoTestService>());
            Assert.Throws<MissingMethodException>(() => factory.Create<IAutoTestService>());
        }

        [Fact]
        public void Create_AutoSubstituteGeneratesUniqueIds()
        {
            var factory = new ObjectFactory();
            var callLog = new CallLog();
            
            factory.SetAutoParrot<IAutoTestService>(callLog, "🔧");
            
            var first = factory.Create<IAutoTestService>();
            var second = factory.Create<IAutoTestService>();
            
            // Should generate different objects with unique IDs
            Assert.NotSame(first, second);
            Assert.True(factory.IsRegistered(first));
            Assert.True(factory.IsRegistered(second));
            
            var firstId = factory.GetRegisteredId(first);
            var secondId = factory.GetRegisteredId(second);
            
            Assert.NotEqual(firstId, secondId);
            Assert.StartsWith("IAutoTestService_", firstId);
            Assert.StartsWith("IAutoTestService_", secondId);
        }
    }

    public class IntegrationTests
    {
        [Fact]
        public void ParrotFromAutoSubstitute_ShouldWorkWithMethodCalls()
        {
            var factory = new ObjectFactory();
            var callLog = new CallLog("""
                🔧 DoWork:
                  🔸 input: "test"
                  🔹 Returns: "processed"
                """);
            
            factory.SetAutoParrot<IAutoTestService>(callLog, "🔧");
            
            var service = factory.Create<IAutoTestService>();
            
            var result = service.DoWork("test");
            
            Assert.Equal("processed", result);
        }

        [Fact]
        public void MultipleAutoSubstitutes_ShouldWorkIndependently()
        {
            var factory = new ObjectFactory();
            var callLog = new CallLog();
            
            factory.SetAutoParrot<IAutoTestService>(callLog, "🔧");
            factory.SetAutoParrot<IAnotherAutoTestService>(callLog, "⚡");
            
            var service1 = factory.Create<IAutoTestService>();
            var service2 = factory.Create<IAnotherAutoTestService>();
            
            Assert.NotSame(service1, service2);
            Assert.IsAssignableFrom<IAutoTestService>(service1);
            Assert.IsAssignableFrom<IAnotherAutoTestService>(service2);
        }
    }

    public class CleanupTests
    {
        [Fact]
        public void ClearAll_ShouldRemoveAutoSubstitutes()
        {
            var factory = new ObjectFactory();
            var callLog = new CallLog();
            
            factory.SetAutoParrot<IAutoTestService>(callLog, "🔧");
            factory.SetAutoParrot<IAnotherAutoTestService>(callLog, "⚡");
            
            factory.ClearAll();
            
            Assert.False(factory.HasAutoParrot<IAutoTestService>());
            Assert.False(factory.HasAutoParrot<IAnotherAutoTestService>());
        }

        [Fact]
        public void Clear_WithSpecificType_ShouldRemoveOnlyThatAutoSubstitute()
        {
            var factory = new ObjectFactory();
            var callLog = new CallLog();
            
            factory.SetAutoParrot<IAutoTestService>(callLog, "🔧");
            factory.SetAutoParrot<IAnotherAutoTestService>(callLog, "⚡");
            
            factory.ClearAutoParrot<IAutoTestService>();
            
            Assert.False(factory.HasAutoParrot<IAutoTestService>());
            Assert.True(factory.HasAutoParrot<IAnotherAutoTestService>());
        }
    }

    // Test interfaces and implementations
    public interface IAutoTestService
    {
        string DoWork(string input);
        int Calculate(int x, int y);
    }

    public interface IAnotherAutoTestService
    {
        bool Process(string data);
    }

    public class AutoTestServiceImpl : IAutoTestService
    {
        public string DoWork(string input) => $"processed: {input}";
        public int Calculate(int x, int y) => x + y;
    }

    public class AnotherAutoTestServiceImpl : IAnotherAutoTestService
    {
        public bool Process(string data) => !string.IsNullOrEmpty(data);
    }
}