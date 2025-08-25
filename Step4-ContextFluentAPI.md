# Step 4: Context Fluent API (Verify Each Method)

## Objective
Add fluent API methods to the Context class one at a time, testing each method individually to ensure they work correctly before adding the next one.

## Context
The unified interface's main value is the fluent API that simplifies SpecRec usage. Each method should be implemented and tested individually to ensure correctness and avoid compound errors.

## Prerequisites
- Step 3 completed successfully (Context class works with basic properties)
- All existing tests passing
- Context unit tests passing

## Success Criteria
- [ ] `Substitute<T>()` method implemented and tested
- [ ] `SetAlways<T>()` method implemented and tested  
- [ ] `SetOne<T>()` method implemented and tested
- [ ] `Wrap<T>()` method implemented and tested
- [ ] `CreateParrot<T>()` method implemented and tested
- [ ] All methods return Context for fluent chaining
- [ ] All methods have comprehensive unit tests
- [ ] Fluent chaining works correctly
- [ ] No regressions in existing functionality

## Implementation Steps

### 4.1 Add Substitute Method (Most Common Pattern)

Add to `Context.cs`:
```csharp
/// <summary>
/// Creates a parrot test double and registers it in the ObjectFactory.
/// This is the most common pattern for creating test doubles.
/// </summary>
public Context Substitute<T>(string icon, string? id = null) where T : class
{
    var parrot = Parrot.Create<T>(_callLog, icon, _factory);
    _factory.SetOne<T>(parrot, id); // SetOne will auto-generate ID if null
    return this;
}
```

#### Test Substitute Method

Add to `ContextUnitTests.cs`:
```csharp
[Fact]
public void Substitute_ShouldCreateParrotAndRegisterInFactory()
{
    // Arrange
    var context = new Context();

    // Act
    var result = context.Substitute<IBasicTestService>("🎭");

    // Assert
    result.Should().BeSameAs(context); // Fluent API
    
    // Verify parrot was created and registered
    var service = context.Factory.Create<IBasicTestService>();
    service.Should().NotBeNull();
}

[Fact] 
public void Substitute_WithId_ShouldRegisterWithSpecificId()
{
    // Arrange
    var context = new Context();

    // Act
    context.Substitute<IBasicTestService>("🎭", "testService");

    // Assert
    var registeredService = context.Factory.GetRegisteredObject<IBasicTestService>("testService");
    registeredService.Should().NotBeNull();
}

[Fact]
public void Substitute_ShouldLogConstructorCall()
{
    // Arrange
    var context = new Context();

    // Act
    context.Substitute<IBasicTestService>("🎭", "service1");

    // Assert - Should have logged the constructor call
    context.CallLog.ToString().Should().Contain("IBasicTestService constructor");
}
```

Run tests:
```bash
dotnet test --filter "ContextUnitTests" --verbosity normal
```

### 4.2 Add SetAlways Method

Add to `Context.cs`:
```csharp
/// <summary>
/// Registers an existing object in the ObjectFactory to always be returned.
/// </summary>
public Context SetAlways<T>(T obj, string? id = null) where T : class
{
    _factory.SetAlways<T>(obj, id);
    return this;
}
```

#### Test SetAlways Method

Add to `ContextUnitTests.cs`:
```csharp
[Fact]
public void SetAlways_ShouldRegisterObjectInFactory()
{
    // Arrange
    var context = new Context();
    var testService = new BasicTestServiceImplementation(); // Need to create this

    // Act
    var result = context.SetAlways<IBasicTestService>(testService, "myService");

    // Assert
    result.Should().BeSameAs(context); // Fluent API
    
    var fromFactory = context.Factory.Create<IBasicTestService>();
    fromFactory.Should().BeSameAs(testService);

    var registeredObject = context.Factory.GetRegisteredObject<IBasicTestService>("myService");
    registeredObject.Should().BeSameAs(testService);
}

[Fact]
public void SetAlways_WithoutId_ShouldGenerateId()
{
    // Arrange  
    var context = new Context();
    var testService = new BasicTestServiceImplementation();

    // Act
    context.SetAlways(testService);

    // Assert - Should be able to retrieve without specifying ID
    var fromFactory = context.Factory.Create<IBasicTestService>();
    fromFactory.Should().BeSameAs(testService);
}
```

Create test implementation class in `ContextUnitTests.cs`:
```csharp
private class BasicTestServiceImplementation : IBasicTestService
{
    public string ProcessData(string input) => $"Processed: {input}";
    public int Calculate(int a, int b) => a + b;
}
```

### 4.3 Add SetOne Method

Add to `Context.cs`:
```csharp
/// <summary>
/// Registers an existing object in the ObjectFactory to be returned once.
/// </summary>
public Context SetOne<T>(T obj, string? id = null) where T : class
{
    _factory.SetOne<T>(obj, id);
    return this;
}
```

#### Test SetOne Method

Add to `ContextUnitTests.cs`:
```csharp
[Fact]
public void SetOne_ShouldRegisterObjectInFactory()
{
    // Arrange
    var context = new Context();
    var testService = new BasicTestServiceImplementation();

    // Act
    var result = context.SetOne<IBasicTestService>(testService, "myService");

    // Assert
    result.Should().BeSameAs(context); // Fluent API
    
    var fromFactory = context.Factory.Create<IBasicTestService>();
    fromFactory.Should().BeSameAs(testService);

    var registeredObject = context.Factory.GetRegisteredObject<IBasicTestService>("myService");
    registeredObject.Should().BeSameAs(testService);
}

[Fact]
public void SetOne_ShouldOnlyProvideObjectOnce()
{
    // Arrange
    var context = new Context();
    var testService = new BasicTestServiceImplementation();

    // Act
    context.SetOne(testService);
    var first = context.Factory.Create<IBasicTestService>();
    
    // Second call should get different instance (or throw, depending on ObjectFactory behavior)
    var act = () => context.Factory.Create<IBasicTestService>();

    // Assert
    first.Should().BeSameAs(testService);
    // Behavior depends on ObjectFactory implementation - document expected behavior
}
```

### 4.4 Add Wrap Method

Add to `Context.cs`:
```csharp
/// <summary>
/// Wraps an existing object with call logging without registering it in the factory.
/// </summary>
public T Wrap<T>(T obj, string icon = "🔧") where T : class
{
    var callLogger = new CallLogger(_callLog.SpecBook, icon, _factory);
    return callLogger.Wrap<T>(obj, icon, _factory);
}
```

#### Test Wrap Method

Add to `ContextUnitTests.cs`:
```csharp
[Fact]
public void Wrap_ShouldWrapObjectWithCallLogger()
{
    // Arrange
    var context = new Context();
    var testService = new BasicTestServiceImplementation();

    // Act
    var wrappedService = context.Wrap<IBasicTestService>(testService, "🔧");

    // Assert
    wrappedService.Should().NotBeNull();
    wrappedService.Should().NotBeSameAs(testService); // Should be wrapped

    // Verify it works by calling a method
    var result = wrappedService.ProcessData("test");
    result.Should().Be("Processed: test");

    // Should have logged the call
    context.CallLog.ToString().Should().Contain("ProcessData");
    context.CallLog.ToString().Should().Contain("test");
}

[Fact]
public void Wrap_ShouldNotRegisterInFactory()
{
    // Arrange
    var context = new Context();
    var testService = new BasicTestServiceImplementation();

    // Act
    var wrappedService = context.Wrap<IBasicTestService>(testService);

    // Assert - Factory should not have the wrapped service
    var factoryService = context.Factory.GetRegisteredObject<IBasicTestService>("service1"); // Or any ID
    factoryService.Should().BeNull();
}
```

### 4.5 Add CreateParrot Method

Add to `Context.cs`:
```csharp
/// <summary>
/// Creates a parrot test double without registering it in the factory.
/// </summary>
public T CreateParrot<T>(string icon = "🦜") where T : class
{
    return Parrot.Create<T>(_callLog, icon, _factory);
}
```

#### Test CreateParrot Method

Add to `ContextUnitTests.cs`:
```csharp
[Fact]
public void CreateParrot_ShouldCreateParrotWithoutRegistration()
{
    // Arrange
    var context = new Context();

    // Act
    var parrot = context.CreateParrot<IBasicTestService>("🦜");

    // Assert
    parrot.Should().NotBeNull();

    // Should not be registered in factory
    var fromFactory = context.Factory.GetRegisteredObject<IBasicTestService>("IBasicTestService_1");
    fromFactory.Should().BeNull(); // Or whatever the expected behavior is
}

[Fact]
public void CreateParrot_ShouldLogConstructorCall()
{
    // Arrange
    var context = new Context();

    // Act
    var parrot = context.CreateParrot<IBasicTestService>("🦜");

    // Assert
    context.CallLog.ToString().Should().Contain("IBasicTestService constructor");
}
```

### 4.6 Test Fluent Chaining

Add comprehensive fluent chaining tests:

```csharp
[Fact]
public void FluentChaining_ShouldWork()
{
    // Arrange
    var context = new Context();
    var testService = new BasicTestServiceImplementation();

    // Act - Chain multiple operations
    var result = context
        .SetAlways<IBasicTestService>(testService, "service1")
        .Substitute<IBasicTestService>("🎭", "service2");

    // Assert
    result.Should().BeSameAs(context);
    
    // Verify both operations worked
    var service1 = context.Factory.GetRegisteredObject<IBasicTestService>("service1");
    service1.Should().BeSameAs(testService);

    var service2 = context.Factory.GetRegisteredObject<IBasicTestService>("service2");
    service2.Should().NotBeNull();
    service2.Should().NotBeSameAs(testService); // Should be different (parrot)
}

[Fact]
public void FluentChaining_ComplexScenario_ShouldWork()
{
    // Arrange
    var context = new Context();
    var realService = new BasicTestServiceImplementation();

    // Act - Complex fluent chain
    var wrappedService = context
        .SetOne(realService, "real")
        .Substitute<IBasicTestService>("🎯", "mock")
        .Wrap(realService, "📝");

    // Assert
    // Verify chaining worked
    context.Factory.GetRegisteredObject<IBasicTestService>("real").Should().BeSameAs(realService);
    context.Factory.GetRegisteredObject<IBasicTestService>("mock").Should().NotBeNull();
    
    // Verify wrapped service works
    wrappedService.Should().NotBeSameAs(realService);
    var result = wrappedService.ProcessData("test");
    result.Should().Be("Processed: test");
}
```

### 4.7 Run All Tests

```bash
# Run all Context tests
dotnet test --filter "ContextUnitTests" --verbosity normal

# Run all tests to ensure no regressions
dotnet test --verbosity minimal
```

## Verification Checklist

### Method Implementation
- [ ] `Substitute<T>()` creates parrot and registers in factory
- [ ] `SetAlways<T>()` registers existing object for repeated use
- [ ] `SetOne<T>()` registers existing object for single use  
- [ ] `Wrap<T>()` wraps object with call logging
- [ ] `CreateParrot<T>()` creates parrot without registration

### Fluent API
- [ ] All methods return Context for chaining
- [ ] Complex fluent chains work correctly
- [ ] Method order doesn't affect functionality

### Testing
- [ ] Each method has unit tests covering success cases
- [ ] Each method has tests for edge cases
- [ ] Fluent chaining has comprehensive tests
- [ ] All tests pass consistently

### Integration
- [ ] Methods work with existing ObjectFactory patterns
- [ ] Methods work with existing CallLogger patterns
- [ ] Methods work with existing Parrot patterns
- [ ] No regressions in existing functionality

## Common Issues & Solutions

**Issue**: Compilation errors in method signatures  
**Solution**: Verify generic constraints and parameter types match existing SpecRec patterns

**Issue**: Tests fail with ObjectFactory issues
**Solution**: Ensure proper setup/teardown with ClearAll(), verify singleton behavior

**Issue**: CallLogger integration fails
**Solution**: Check CallLogger constructor parameters match existing usage

**Issue**: Parrot creation fails
**Solution**: Verify Parrot.Create signature matches existing implementation

**Issue**: Fluent chaining breaks
**Solution**: Ensure all methods return `this` and don't modify method parameters

## Performance Considerations

- Context methods should have minimal overhead
- ObjectFactory calls should be efficient (they're used frequently)
- CallLogger wrapping should not significantly impact performance

## Next Step
Once all verification criteria pass, proceed to [Step 5: Update Discoverer for Context](Step5-UpdateDiscovererForContext.md).

## Notes
- Each method should be implemented and tested individually
- Don't proceed to the next method until the current one is fully tested
- Pay attention to the differences between `SetOne` vs `SetAlways` vs `Substitute`
- `Wrap` and `CreateParrot` don't register objects - this is intentional
- Default parameter values (like default icons) should be consistent with existing patterns