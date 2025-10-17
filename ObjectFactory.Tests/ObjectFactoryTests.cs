using static ObjectFactory.GlobalObjectFactory;

namespace ObjectFactory.Tests;

public class ObjectFactoryTests
{
    public class InstanceManagement
    {
        [Fact]
        public void Instance_ReturnsSameInstance()
        {
            var instance1 = ObjectFactory.Instance();
            var instance2 = ObjectFactory.Instance();

            Assert.Same(instance1, instance2);
        }
    }

    public class BasicCreateOperations
    {
        [Fact]
        public void Create_WithoutSetup_CreatesDefaultInstance()
        {
            var factory = new ObjectFactory();

            var result = factory.Create<TestClass>();

            Assert.NotNull(result);
            Assert.IsType<TestClass>(result);
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
        public void CreateGeneric_WithInterface_CreatesConcreteType()
        {
            var factory = new ObjectFactory();

            var result = factory.Create<ITestInterface, TestImplementation>();

            Assert.IsType<TestImplementation>(result);
            Assert.IsAssignableFrom<ITestInterface>(result);
        }
    }

    public class SetOneBehavior
    {
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
        public void CreateGeneric_WithSetOne_ReturnsQueuedObject()
        {
            var factory = new ObjectFactory();
            var mockObj = new MockTestImplementation();

            factory.SetOne<ITestInterface>(mockObj);

            var result = factory.Create<ITestInterface, TestImplementation>();

            Assert.Same(mockObj, result);
        }
    }

    public class SetAlwaysBehavior
    {
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
    }

    public class ClearOperations
    {
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
    }

    public class ConstructorCalledWithTests
    {
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
        public void Create_WithConstructorCalledWith_CallsMethodViaDelegation()
        {
            var factory = new ObjectFactory();
            var mockObj = new MockTestImplementation();

            factory.SetOne<MockTestImplementation>(mockObj);

            factory.Create<MockTestImplementation>("arg1", 123);

            Assert.NotNull(mockObj.LastConstructorArgs);
            Assert.Equal(2, mockObj.LastConstructorArgs.Length);
            Assert.Equal("arg1", mockObj.LastConstructorArgs[0]);
            Assert.Equal(123, mockObj.LastConstructorArgs[1]);
        }

        [Fact]
        public async Task CreateGeneric_WithMock_CallsConstructorCalledWithParameterDetails()
        {
            var factory = new ObjectFactory();
            var mockObj = new MockTestImplementation();

            factory.SetOne<ITestInterface>(mockObj);

            factory.Create<ITestInterface, TestClassWithConstructor>("testArg", 42);

            Assert.NotNull(mockObj.LastParameterDetails);
            var parameterStrings = string.Join("\n", mockObj.LastParameterDetails.Select(p => p.ToString()));

            await Verify(parameterStrings);
        }

        [Fact]
        public async Task Create_WithTestClassWithConstructor_ExtractsParameterNames()
        {
            var factory = new ObjectFactory();
            var mockObj = new MockTestClassWithConstructor();

            factory.SetOne<TestClassWithConstructor>(mockObj);

            factory.Create<TestClassWithConstructor>("paramName", 99);

            Assert.NotNull(mockObj.LastParameterDetails);
            var parameterStrings = string.Join("\n", mockObj.LastParameterDetails.Select(p => p.ToString()));

            await Verify(parameterStrings);
        }

        [Fact]
        public async Task Create_WithNoMatchingConstructor_UsesGenericParameterNames()
        {
            var factory = new ObjectFactory();
            var mockObj = new MockTestImplementation();

            factory.SetOne<ITestInterface>(mockObj);

            factory.Create<ITestInterface, TestImplementation>("unexpected", 42);

            Assert.NotNull(mockObj.LastParameterDetails);
            var parameterStrings = string.Join("\n", mockObj.LastParameterDetails.Select(p => p.ToString()));

            await Verify(parameterStrings);
        }
    }

    public class InheritanceScenarios
    {
        [Fact]
        public void Create_WithParentType_SetChildInstance_ReturnsChildAsParent()
        {
            var factory = new ObjectFactory();
            var childInstance = new ChildClass("child value");

            factory.SetOne<ParentClass>(childInstance);

            var result = factory.Create<ParentClass>();

            Assert.Same(childInstance, result);
            Assert.IsType<ChildClass>(result);
            Assert.Equal("child value", ((ChildClass)result).ChildProperty);
        }

        [Fact]
        public void Create_WithParentType_SetAlwaysChild_AlwaysReturnsChild()
        {
            var factory = new ObjectFactory();
            var childInstance = new ChildClass("always child");

            factory.SetAlways<ParentClass>(childInstance);

            var result1 = factory.Create<ParentClass>();
            var result2 = factory.Create<ParentClass>();

            Assert.Same(childInstance, result1);
            Assert.Same(childInstance, result2);
            Assert.IsType<ChildClass>(result1);
            Assert.Equal("always child", ((ChildClass)result1).ChildProperty);
        }

        [Fact]
        public void Create_WithParentType_NoSetup_CreatesParentDirectly()
        {
            var factory = new ObjectFactory();

            var result = factory.Create<ParentClass>();

            Assert.NotNull(result);
            Assert.IsType<ParentClass>(result);
            Assert.IsNotType<ChildClass>(result);
        }

        [Fact]
        public void Create_WithParentType_ChildWithConstructorArgs_LogsCorrectly()
        {
            var factory = new ObjectFactory();
            var mockChild = new MockChildClass();

            factory.SetOne<ParentClass>(mockChild);

            factory.Create<ParentClass>("parent arg", 42);

            Assert.NotNull(mockChild.LastConstructorArgs);
            Assert.Equal(2, mockChild.LastConstructorArgs.Length);
            Assert.Equal("parent arg", mockChild.LastConstructorArgs[0]);
            Assert.Equal(42, mockChild.LastConstructorArgs[1]);
        }

        [Fact]
        public void Create_WithParentType_SetOnePriorityOverSetAlways()
        {
            var factory = new ObjectFactory();
            var alwaysChild = new ChildClass("always");
            var queuedChild = new ChildClass("queued");

            factory.SetAlways<ParentClass>(alwaysChild);
            factory.SetOne<ParentClass>(queuedChild);

            var result1 = factory.Create<ParentClass>();
            var result2 = factory.Create<ParentClass>();

            // First call should return queued child
            Assert.Same(queuedChild, result1);
            Assert.Equal("queued", ((ChildClass)result1).ChildProperty);

            // Second call should return always child (queue is empty)
            Assert.Same(alwaysChild, result2);
            Assert.Equal("always", ((ChildClass)result2).ChildProperty);
        }
    }

    public class GlobalObjectFactoryTests
    {
        [Fact]
        public void GlobalObjectFactory_Create_UsesObjectFactoryInstance()
        {
            var testObj = new TestClass();
            ObjectFactory.Instance().SetOne<TestClass>(testObj);

            var result = GlobalObjectFactory.Create<TestClass>();

            Assert.Same(testObj, result);

            // Cleanup
            ObjectFactory.Instance().ClearAll();
        }

        [Fact]
        public void GlobalObjectFactory_CreateGeneric_UsesObjectFactoryInstance()
        {
            var mockObj = new MockTestImplementation();
            ObjectFactory.Instance().SetOne<ITestInterface>(mockObj);

            var result = GlobalObjectFactory.Create<ITestInterface, TestImplementation>();

            Assert.Same(mockObj, result);

            // Cleanup
            ObjectFactory.Instance().ClearAll();
        }

        [Fact]
        public void GlobalObjectFactory_Create_PassesConstructorArgs()
        {
            var result = GlobalObjectFactory.Create<TestClassWithConstructor>("test", 42);

            Assert.Equal("test", result.Name);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void DirectCreate_WithStaticImport_WorksWithoutClassPrefix()
        {
            var result = Create<TestClassWithConstructor>("direct", 99);

            Assert.Equal("direct", result.Name);
            Assert.Equal(99, result.Value);
        }
    }

    // Test helper classes
    public class TestClass
    {
    }

    public class TestClassWithConstructor : ITestInterface
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
        public ConstructorParameterInfo[]? LastParameterDetails { get; private set; }

        public void ConstructorCalledWith(ConstructorParameterInfo[] parameters)
        {
            LastParameterDetails = parameters;
            LastConstructorArgs = parameters.Select(p => p.Value).ToArray()!;
        }
    }

    public class AnotherTestClass
    {
    }

    public class ParentClass
    {
        public string ParentProperty { get; set; } = "parent";
    }

    public class ChildClass : ParentClass
    {
        public string ChildProperty { get; }

        public ChildClass(string childValue)
        {
            ChildProperty = childValue;
        }
    }

    public class MockChildClass : ParentClass, IConstructorCalledWith
    {
        public object[]? LastConstructorArgs { get; private set; }
        public ConstructorParameterInfo[]? LastParameterDetails { get; private set; }

        public void ConstructorCalledWith(ConstructorParameterInfo[] parameters)
        {
            LastParameterDetails = parameters;
            LastConstructorArgs = parameters.Select(p => p.Value).ToArray()!;
        }
    }

    public class MockTestClassWithConstructor : TestClassWithConstructor, IConstructorCalledWith
    {
        public object[]? LastConstructorArgs { get; private set; }
        public ConstructorParameterInfo[]? LastParameterDetails { get; private set; }

        public MockTestClassWithConstructor() : base("default", 0)
        {
        }

        public void ConstructorCalledWith(ConstructorParameterInfo[] parameters)
        {
            LastParameterDetails = parameters;
            LastConstructorArgs = parameters.Select(p => p.Value).ToArray()!;
        }
    }
}
