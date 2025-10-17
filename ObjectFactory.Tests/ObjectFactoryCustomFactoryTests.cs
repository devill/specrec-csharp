namespace ObjectFactory.Tests;

public class ObjectFactoryCustomFactoryTests
{
    [Fact]
    public void SetFactory_WithValidFactory_ShouldUseCustomFactory()
    {
        var factory = new ObjectFactory();
        var customInstance = new TestService();

        factory.SetFactory<ITestService>(args => customInstance);

        var result = factory.Create<ITestService>();

        Assert.Same(customInstance, result);
    }

    [Fact]
    public void SetFactory_WithParameterizedFactory_ShouldPassArguments()
    {
        var factory = new ObjectFactory();

        factory.SetFactory<ITestService>(args =>
            new TestService((string)args[0], (int)args[1]));

        var result = factory.Create<ITestService>("test", 42);

        Assert.NotNull(result);
        Assert.IsType<TestService>(result);
        Assert.Equal("test", ((TestService)result).Name);
        Assert.Equal(42, ((TestService)result).Value);
    }

    [Fact]
    public void HasCustomFactory_WithoutFactory_ShouldReturnFalse()
    {
        var factory = new ObjectFactory();

        Assert.False(factory.HasCustomFactory<ITestService>());
    }

    [Fact]
    public void HasCustomFactory_WithFactory_ShouldReturnTrue()
    {
        var factory = new ObjectFactory();
        factory.SetFactory<ITestService>(args => new TestService());

        Assert.True(factory.HasCustomFactory<ITestService>());
    }

    [Fact]
    public void ClearFactory_ShouldRemoveCustomFactory()
    {
        var factory = new ObjectFactory();
        factory.SetFactory<ITestService>(args => new TestService());

        factory.ClearFactory<ITestService>();

        Assert.False(factory.HasCustomFactory<ITestService>());
    }

    [Fact]
    public void SetFactory_WithMultipleTypes_ShouldWorkIndependently()
    {
        var factory = new ObjectFactory();
        var testInstance = new TestService();
        var anotherInstance = new AnotherService();

        factory.SetFactory<ITestService>(args => testInstance);
        factory.SetFactory<IAnotherService>(args => anotherInstance);

        var result1 = factory.Create<ITestService>();
        var result2 = factory.Create<IAnotherService>();

        Assert.Same(testInstance, result1);
        Assert.Same(anotherInstance, result2);
    }

    [Fact]
    public void SetFactory_OverridesDefaultCreation()
    {
        var factory = new ObjectFactory();
        var customInstance = new ConcreteService();

        factory.SetFactory<ConcreteService>(args => customInstance);

        var result = factory.Create<ConcreteService>();

        Assert.Same(customInstance, result);
    }

    [Fact]
    public void SetFactory_WithSetOne_SetOneTakesPrecedence()
    {
        var factory = new ObjectFactory();
        var factoryInstance = new TestService("factory", 1);
        var queuedInstance = new TestService("queued", 2);

        factory.SetFactory<ITestService>(args => factoryInstance);
        factory.SetOne<ITestService>(queuedInstance);

        var result = factory.Create<ITestService>();

        Assert.Same(queuedInstance, result);
    }

    [Fact]
    public void SetFactory_WithSetAlways_SetAlwaysTakesPrecedence()
    {
        var factory = new ObjectFactory();
        var factoryInstance = new TestService("factory", 1);
        var alwaysInstance = new TestService("always", 2);

        factory.SetFactory<ITestService>(args => factoryInstance);
        factory.SetAlways<ITestService>(alwaysInstance);

        var result1 = factory.Create<ITestService>();
        var result2 = factory.Create<ITestService>();

        Assert.Same(alwaysInstance, result1);
        Assert.Same(alwaysInstance, result2);
    }

    [Fact]
    public void SetFactory_AfterQueueEmpty_UsesFactory()
    {
        var factory = new ObjectFactory();
        var factoryInstance1 = new TestService("factory1", 1);
        var factoryInstance2 = new TestService("factory2", 2);
        var queuedInstance = new TestService("queued", 3);

        var callCount = 0;
        factory.SetFactory<ITestService>(args =>
        {
            callCount++;
            return callCount == 1 ? factoryInstance1 : factoryInstance2;
        });

        factory.SetOne<ITestService>(queuedInstance);

        var result1 = factory.Create<ITestService>();
        var result2 = factory.Create<ITestService>();
        var result3 = factory.Create<ITestService>();

        Assert.Same(queuedInstance, result1);
        Assert.Same(factoryInstance1, result2);
        Assert.Same(factoryInstance2, result3);
    }

    [Fact]
    public void ClearAll_ShouldRemoveAllCustomFactories()
    {
        var factory = new ObjectFactory();
        factory.SetFactory<ITestService>(args => new TestService());
        factory.SetFactory<IAnotherService>(args => new AnotherService());

        factory.ClearAll();

        Assert.False(factory.HasCustomFactory<ITestService>());
        Assert.False(factory.HasCustomFactory<IAnotherService>());
    }

    [Fact]
    public void Clear_SpecificType_ShouldOnlyRemoveThatFactory()
    {
        var factory = new ObjectFactory();
        factory.SetFactory<ITestService>(args => new TestService());
        factory.SetFactory<IAnotherService>(args => new AnotherService());

        factory.ClearFactory<ITestService>();

        Assert.False(factory.HasCustomFactory<ITestService>());
        Assert.True(factory.HasCustomFactory<IAnotherService>());
    }

    [Fact]
    public void SetFactory_WithGenericCreate_ShouldUseFactory()
    {
        var factory = new ObjectFactory();
        var customInstance = new TestService();

        factory.SetFactory<ITestService>(args => customInstance);

        var result = factory.Create<ITestService, TestService>();

        Assert.Same(customInstance, result);
    }

    [Fact]
    public void SetFactory_CalledMultipleTimes_LatestWins()
    {
        var factory = new ObjectFactory();
        var firstInstance = new TestService("first", 1);
        var secondInstance = new TestService("second", 2);

        factory.SetFactory<ITestService>(args => firstInstance);
        factory.SetFactory<ITestService>(args => secondInstance);

        var result = factory.Create<ITestService>();

        Assert.Same(secondInstance, result);
    }

    // Test helper classes
    public interface ITestService { }

    public class TestService : ITestService
    {
        public string Name { get; }
        public int Value { get; }

        public TestService() : this("default", 0) { }

        public TestService(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    public interface IAnotherService { }

    public class AnotherService : IAnotherService
    {
        public string Name { get; set; } = "AnotherService";
    }

    public class ConcreteService
    {
        public string Name { get; set; } = "ConcreteService";
    }
}
