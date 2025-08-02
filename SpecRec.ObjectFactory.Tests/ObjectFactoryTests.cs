using System;

namespace SpecRec.Tests;

public class ObjectFactoryTests
{
    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        var instance1 = ObjectFactory.Instance();
        var instance2 = ObjectFactory.Instance();
        
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Create_WithoutSetup_CreatesDefaultInstance()
    {
        var factory = new ObjectFactory();
        
        var result = factory.Create<TestClass>();
        
        Assert.NotNull(result);
        Assert.IsType<TestClass>(result);
    }
    
    [Fact]
    public void CreateGeneric_WithInterface_CreatesConcreteType()
    {
        var factory = new ObjectFactory();
        
        var result = factory.Create<ITestInterface, TestImplementation>();
        
        Assert.IsType<TestImplementation>(result);
        Assert.IsAssignableFrom<ITestInterface>(result);
    }

    [Fact]
    public void Create_WithConstructorArgs_PassesArgsToConstructor()
    {
        var factory = new ObjectFactory();
        
        var result = factory.Create<TestClassWithConstructor>("test", 42);
        
        Assert.Equal("test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void SetOne_SetsQueuedObject_ReturnedByCreate()
    {
        var factory = new ObjectFactory();
        var testObj = new TestClass();
        
        factory.SetOne<TestClass>(testObj);
        var result = factory.Create<TestClass>();
        
        Assert.Same(testObj, result);
    }

    [Fact]
    public void SetOne_MultipleObjects_ReturnedInOrder()
    {
        var factory = new ObjectFactory();
        var obj1 = new TestClass();
        var obj2 = new TestClass();
        
        factory.SetOne<TestClass>(obj1);
        factory.SetOne<TestClass>(obj2);
        
        var result1 = factory.Create<TestClass>();
        var result2 = factory.Create<TestClass>();
        
        Assert.Same(obj1, result1);
        Assert.Same(obj2, result2);
    }

    [Fact]
    public void SetOne_AfterQueueEmpty_CreatesDefault()
    {
        var factory = new ObjectFactory();
        var testObj = new TestClass();
        
        factory.SetOne<TestClass>(testObj);
        factory.Create<TestClass>(); // Consume queued object
        
        var result = factory.Create<TestClass>();
        
        Assert.NotSame(testObj, result);
        Assert.IsType<TestClass>(result);
    }

    [Fact]
    public void SetAlways_SetsAlwaysObject_AlwaysReturned()
    {
        var factory = new ObjectFactory();
        var testObj = new TestClass();
        
        factory.SetAlways<TestClass>(testObj);
        
        var result1 = factory.Create<TestClass>();
        var result2 = factory.Create<TestClass>();
        
        Assert.Same(testObj, result1);
        Assert.Same(testObj, result2);
    }

    [Fact]
    public void SetOne_OverridesSetAlways()
    {
        var factory = new ObjectFactory();
        var alwaysObj = new TestClass();
        var queuedObj = new TestClass();
        
        factory.SetAlways<TestClass>(alwaysObj);
        factory.SetOne<TestClass>(queuedObj);
        
        var result = factory.Create<TestClass>();
        
        Assert.Same(queuedObj, result);
    }

    [Fact]
    public void CreateGeneric_WithSetOne_ReturnsQueuedObject()
    {
        var factory = new ObjectFactory();
        var mockObj = new MockTestImplementation();
        
        factory.SetOne<ITestInterface>(mockObj);
        
        var result = factory.Create<ITestInterface, TestImplementation>();
        
        Assert.Same(mockObj, result);
    }

    [Fact]
    public void CreateGeneric_WithConstructorCalledWith_CallsMethod()
    {
        var factory = new ObjectFactory();
        var mockObj = new MockTestImplementation();
        
        factory.SetOne<ITestInterface>(mockObj);
        
        factory.Create<ITestInterface, TestImplementation>("arg1", 123);
        
        Assert.NotNull(mockObj.LastConstructorArgs);
        Assert.Equal(2, mockObj.LastConstructorArgs.Length);
        Assert.Equal("arg1", mockObj.LastConstructorArgs[0]);
        Assert.Equal(123, mockObj.LastConstructorArgs[1]);
    }

    [Fact]
    public void CreateGeneric_WithSetAlwaysAndConstructorCalledWith_CallsMethod()
    {
        var factory = new ObjectFactory();
        var mockObj = new MockTestImplementation();
        
        factory.SetAlways<ITestInterface>(mockObj);
        
        factory.Create<ITestInterface, TestImplementation>("arg1", 123);
        
        Assert.NotNull(mockObj.LastConstructorArgs);
        Assert.Equal(2, mockObj.LastConstructorArgs.Length);
        Assert.Equal("arg1", mockObj.LastConstructorArgs[0]);
        Assert.Equal(123, mockObj.LastConstructorArgs[1]);
    }

    [Fact]
    public void Clear_RemovesAlwaysAndQueuedObjects()
    {
        var factory = new ObjectFactory();
        var alwaysObj = new TestClass();
        var queuedObj = new TestClass();
        
        factory.SetAlways<TestClass>(alwaysObj);
        factory.SetOne<TestClass>(queuedObj);
        
        factory.Clear<TestClass>();
        
        var result = factory.Create<TestClass>();
        
        Assert.NotSame(alwaysObj, result);
        Assert.NotSame(queuedObj, result);
        Assert.IsType<TestClass>(result);
    }
    
    [Fact]
    public void Clear_DoesNotRemoveOtherTypes()
    {
        var factory = new ObjectFactory();
        var alwaysObj = new TestClass();
        var queuedObj = new TestClass();
        
        factory.SetAlways<TestClass>(alwaysObj);
        factory.SetOne<TestClass>(queuedObj);
        
        factory.Clear<AnotherTestClass>();

        Assert.Same(queuedObj, factory.Create<TestClass>());
        Assert.Same(alwaysObj, factory.Create<TestClass>());
    }

    [Fact]
    public void ClearAll_RemovesAllObjects()
    {
        var factory = new ObjectFactory();
        var testObj1 = new TestClass();
        var testObj2 = new AnotherTestClass();
        
        factory.SetAlways<TestClass>(testObj1);
        factory.SetAlways<AnotherTestClass>(testObj2);
        
        factory.ClearAll();
        
        var result1 = factory.Create<TestClass>();
        var result2 = factory.Create<AnotherTestClass>();
        
        Assert.NotSame(testObj1, result1);
        Assert.NotSame(testObj2, result2);
    }

    // Test helper classes
    public class TestClass
    {
    }

    public class TestClassWithConstructor
    {
        public string Name { get; }
        public int Value { get; }

        public TestClassWithConstructor(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    public interface ITestInterface
    {
    }

    public class TestImplementation : ITestInterface
    {
    }

    public class MockTestImplementation : ITestInterface, IConstructorCalledWith
    {
        public object[]? LastConstructorArgs { get; private set; }

        public void ConstructorCalledWith(params object[] args)
        {
            LastConstructorArgs = args;
        }
    }

    public class AnotherTestClass
    {
    }

    // Tests for ObjectCreation static convenience class
    [Fact]
    public void ObjectCreation_Create_UsesObjectFactoryInstance()
    {
        var testObj = new TestClass();
        ObjectFactory.Instance().SetOne<TestClass>(testObj);
        
        var result = ObjectCreation.Create<TestClass>();
        
        Assert.Same(testObj, result);
        
        // Cleanup
        ObjectFactory.Instance().ClearAll();
    }

    [Fact]
    public void ObjectCreation_CreateGeneric_UsesObjectFactoryInstance()
    {
        var mockObj = new TestServiceMock();
        ObjectFactory.Instance().SetOne<ITestService>(mockObj);
        
        var result = ObjectCreation.Create<ITestService, TestServiceImpl>();
        
        Assert.Same(mockObj, result);
        
        // Cleanup
        ObjectFactory.Instance().ClearAll();
    }

    [Fact]
    public void ObjectCreation_Create_PassesConstructorArgs()
    {
        var result = ObjectCreation.Create<TestClassWithConstructor>("test", 42);
        
        Assert.Equal("test", result.Name);
        Assert.Equal(42, result.Value);
    }

    // Additional test helper classes for ObjectCreation tests
    public interface ITestService
    {
    }

    public class TestServiceImpl : ITestService
    {
    }

    public class TestServiceMock : ITestService
    {
    }

}