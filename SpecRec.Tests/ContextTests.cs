namespace SpecRec.Tests;

public class ContextTests
{
    public class BasicInstantiationTests
    {
        [Fact]
        public void Context_WithCallLogAndFactory_ShouldInitializeCorrectly()
        {
            var callLog = new CallLog();
            var factory = new ObjectFactory();
            
            var context = new Context(callLog, factory);
            
            Assert.NotNull(context);
            Assert.Equal("DefaultCase", context.ToString());
        }

        [Fact]
        public void Context_WithTestCaseName_ShouldReturnCorrectToString()
        {
            var callLog = new CallLog();
            var factory = new ObjectFactory();
            
            var context = new Context(callLog, factory, "EventCreation");
            
            Assert.Equal("EventCreation", context.ToString());
        }
    }

    public class SubstituteMethodTests
    {
        [Fact]
        public void Substitute_WithInterface_ShouldConfigureAutoParrot()
        {
            var callLog = new CallLog();
            var factory = new ObjectFactory();
            var context = new Context(callLog, factory);
            
            var result = context.Substitute<IContextTestService>("🔧");
            
            Assert.Same(context, result); // Should return this for fluent interface
            Assert.True(factory.HasAutoParrot<IContextTestService>());
        }

        [Fact]
        public void Substitute_WithDefaultIcon_ShouldUseDefault()
        {
            var callLog = new CallLog();
            var factory = new ObjectFactory();
            var context = new Context(callLog, factory);
            
            context.Substitute<IContextTestService>();
            
            Assert.True(factory.HasAutoParrot<IContextTestService>());
        }

        [Fact]
        public void Substitute_ChainedCalls_ShouldWorkCorrectly()
        {
            var callLog = new CallLog();
            var factory = new ObjectFactory();
            var context = new Context(callLog, factory);
            
            var result = context.Substitute<IContextTestService>("🔧")
                               .Substitute<IAnotherContextTestService>("⚡");
            
            Assert.Same(context, result);
            Assert.True(factory.HasAutoParrot<IContextTestService>());
            Assert.True(factory.HasAutoParrot<IAnotherContextTestService>());
        }
    }

    public class WrapMethodTests
    {
        [Fact]
        public void Wrap_WithObject_ShouldReturnWrappedInstance()
        {
            var callLog = new CallLog();
            var factory = new ObjectFactory();
            var context = new Context(callLog, factory);
            var service = new ContextTestServiceImpl();
            
            var wrapped = context.Wrap<IContextTestService>(service, "🔧");
            
            Assert.NotNull(wrapped);
            Assert.IsAssignableFrom<IContextTestService>(wrapped);
            Assert.NotSame(service, wrapped); // Should be wrapped
        }

        [Fact]
        public void Wrap_WithDefaultIcon_ShouldWork()
        {
            var callLog = new CallLog();
            var factory = new ObjectFactory();
            var context = new Context(callLog, factory);
            var service = new ContextTestServiceImpl();
            
            var wrapped = context.Wrap<IContextTestService>(service);
            
            Assert.NotNull(wrapped);
            Assert.IsAssignableFrom<IContextTestService>(wrapped);
        }
    }

    public class ParrotMethodTests
    {
        [Fact]
        public void Parrot_ShouldCreateParrotInstance()
        {
            var callLog = new CallLog();
            var factory = new ObjectFactory();
            var context = new Context(callLog, factory);
            
            var parrot = context.Parrot<IContextTestService>("🦜");
            
            Assert.NotNull(parrot);
            Assert.IsAssignableFrom<IContextTestService>(parrot);
        }

        [Fact]
        public void Parrot_WithDefaultIcon_ShouldWork()
        {
            var callLog = new CallLog();
            var factory = new ObjectFactory();
            var context = new Context(callLog, factory);
            
            var parrot = context.Parrot<IContextTestService>();
            
            Assert.NotNull(parrot);
            Assert.IsAssignableFrom<IContextTestService>(parrot);
        }
    }

    public class ObjectFactoryDelegationTests
    {
        [Fact]
        public void SetAlways_ShouldDelegateToFactory()
        {
            var callLog = new CallLog();
            var factory = new ObjectFactory();
            var context = new Context(callLog, factory);
            var service = new ContextTestServiceImpl();
            
            var result = context.SetAlways<IContextTestService>(service, "testId");
            
            Assert.Same(context, result); // Should return this for fluent interface
            Assert.Equal("testId", factory.GetRegisteredId(service));
        }

        [Fact]
        public void SetOne_ShouldDelegateToFactory()
        {
            var callLog = new CallLog();
            var factory = new ObjectFactory();
            var context = new Context(callLog, factory);
            var service = new ContextTestServiceImpl();
            
            var result = context.SetOne<IContextTestService>(service, "testId");
            
            Assert.Same(context, result);
            Assert.Equal("testId", factory.GetRegisteredId(service));
        }

        [Fact]
        public void Register_ShouldDelegateToFactory()
        {
            var callLog = new CallLog();
            var factory = new ObjectFactory();
            var context = new Context(callLog, factory);
            var service = new ContextTestServiceImpl();
            
            var result = context.Register<IContextTestService>(service, "testId");
            
            Assert.Same(context, result);
            Assert.Equal("testId", factory.GetRegisteredId(service));
        }

        [Fact]
        public void SetAlways_WithoutId_ShouldGenerateAutoId()
        {
            var callLog = new CallLog();
            var factory = new ObjectFactory();
            var context = new Context(callLog, factory);
            var service = new ContextTestServiceImpl();
            
            context.SetAlways<IContextTestService>(service);
            
            var id = factory.GetRegisteredId(service);
            Assert.NotNull(id);
            Assert.StartsWith("ContextTestServiceImpl_", id);
        }
    }

    public class FluentInterfaceTests
    {
        [Fact]
        public void ComplexFluentChain_ShouldWorkCorrectly()
        {
            var callLog = new CallLog();
            var factory = new ObjectFactory();
            var context = new Context(callLog, factory);
            var service1 = new ContextTestServiceImpl();
            var service2 = new AnotherContextTestServiceImpl();
            
            var result = context
                .Substitute<IThirdContextTestService>("🔧")
                .SetAlways<IContextTestService>(service1, "service1")
                .SetOne<IAnotherContextTestService>(service2, "service2")
                .Register<IContextTestService>(new ContextTestServiceImpl(), "service3");
            
            Assert.Same(context, result);
            Assert.True(factory.HasAutoParrot<IThirdContextTestService>());
            Assert.Equal("service1", factory.GetRegisteredId(service1));
            Assert.Equal("service2", factory.GetRegisteredId(service2));
        }
    }

    public class IntegrationTests
    {
        [Fact]
        public void Context_WithSubstituteAndFactoryCreate_ShouldCreateParrot()
        {
            var callLog = new CallLog("""
                🔧 DoWork:
                  🔸 input: "test"
                  🔹 Returns: "processed"
                """);
            var factory = new ObjectFactory();
            var context = new Context(callLog, factory);
            
            context.Substitute<IContextTestService>("🔧");
            var service = factory.Create<IContextTestService>();
            
            var result = service.DoWork("test");
            
            Assert.Equal("processed", result);
        }

        [Fact]
        public void Context_WithWrapAndLogging_ShouldLogCalls()
        {
            var callLog = new CallLog();
            var factory = new ObjectFactory();
            var context = new Context(callLog, factory);
            var service = new ContextTestServiceImpl();
            
            var wrapped = context.Wrap<IContextTestService>(service, "🔧");
            wrapped.DoWork("test");
            
            var log = callLog.ToString();
            Assert.Contains("🔧 DoWork:", log);
            Assert.Contains("input: \"test\"", log);
        }
    }

    // Test interfaces and implementations
    public interface IContextTestService
    {
        string DoWork(string input);
        int Calculate(int x, int y);
    }

    public interface IAnotherContextTestService
    {
        bool Process(string data);
    }

    public interface IThirdContextTestService
    {
        void Execute();
    }

    public class ContextTestServiceImpl : IContextTestService
    {
        public string DoWork(string input) => $"processed: {input}";
        public int Calculate(int x, int y) => x + y;
    }

    public class AnotherContextTestServiceImpl : IAnotherContextTestService
    {
        public bool Process(string data) => !string.IsNullOrEmpty(data);
    }
}