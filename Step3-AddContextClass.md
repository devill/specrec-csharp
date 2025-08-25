# Step 3: Add Context Class (Verify Context Creation)

## Objective
Create the `Context` class with basic properties and verify it can be instantiated and used independently of the test discovery system.

## Context
The unified interface centers around a `Context` class that provides a fluent API for SpecRec operations. Before integrating it with the discoverer, we need to ensure the Context class itself works correctly.

## Prerequisites
- Step 2 completed successfully (basic `[SpecRec]` attribute works with `CallLog`)
- All existing tests passing
- Understanding of fluent API patterns

## Success Criteria
- [ ] `Context` class created with required properties
- [ ] Context can be instantiated manually
- [ ] Context provides access to `CallLog` and `ObjectFactory`
- [ ] Context `ToString()` method returns appropriate test case name
- [ ] Comprehensive unit tests for Context class pass
- [ ] No regressions in existing functionality

## Implementation Steps

### 3.1 Create Context Class Structure

Create `SpecRec/Context.cs`:
```csharp
namespace SpecRec
{
    public class Context
    {
        private readonly CallLog _callLog;
        private readonly ObjectFactory _factory;
        private string? _testCaseName;

        // Public constructor for manual usage and testing
        public Context()
        {
            _callLog = new CallLog();
            _factory = ObjectFactory.Instance();
        }

        // Internal constructor for discoverer usage
        internal Context(CallLog callLog, ObjectFactory factory, string? testCaseName = null)
        {
            _callLog = callLog ?? throw new ArgumentNullException(nameof(callLog));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _testCaseName = testCaseName;
        }

        // Public properties for test access
        public CallLog CallLog => _callLog;
        public ObjectFactory Factory => _factory;

        // Internal properties for discoverer
        internal string? TestCaseName 
        { 
            get => _testCaseName; 
            set => _testCaseName = value; 
        }

        // Display name for test parameters  
        public override string ToString() => _testCaseName ?? "DefaultCase";
    }
}
```

### 3.2 Create Comprehensive Unit Tests

Create `SpecRec.Tests/ContextUnitTests.cs`:
```csharp
using Xunit;
using FluentAssertions;

namespace SpecRec.Tests
{
    public class ContextUnitTests : IDisposable
    {
        public ContextUnitTests()
        {
            ObjectFactory.Instance().ClearAll();
        }

        public void Dispose()
        {
            ObjectFactory.Instance().ClearAll();
        }

        [Fact]
        public void Context_DefaultConstructor_ShouldInitializeCorrectly()
        {
            // Act
            var context = new Context();

            // Assert
            context.Should().NotBeNull();
            context.CallLog.Should().NotBeNull();
            context.Factory.Should().NotBeNull();
            context.ToString().Should().Be("DefaultCase");
        }

        [Fact]
        public void Context_WithCallLogConstructor_ShouldUseProvidedInstances()
        {
            // Arrange
            var callLog = new CallLog();
            var factory = ObjectFactory.Instance();

            // Act
            var context = new Context(callLog, factory, "TestCase1");

            // Assert
            context.CallLog.Should().BeSameAs(callLog);
            context.Factory.Should().BeSameAs(factory);
            context.ToString().Should().Be("TestCase1");
        }

        [Fact]
        public void Context_InternalConstructor_WithNullCallLog_ShouldThrow()
        {
            // Arrange & Act
            var act = () => new Context(null!, ObjectFactory.Instance());

            // Assert
            act.Should().Throw<ArgumentNullException>()
               .WithMessage("*callLog*");
        }

        [Fact]
        public void Context_InternalConstructor_WithNullFactory_ShouldThrow()
        {
            // Arrange & Act  
            var act = () => new Context(new CallLog(), null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
               .WithMessage("*factory*");
        }

        [Fact]
        public void ToString_WithNullTestCaseName_ShouldReturnDefaultCase()
        {
            // Arrange
            var context = new Context();

            // Act & Assert
            context.ToString().Should().Be("DefaultCase");
        }

        [Fact]
        public void ToString_WithTestCaseName_ShouldReturnTestCaseName()
        {
            // Arrange
            var context = new Context(new CallLog(), ObjectFactory.Instance(), "MyTestCase");

            // Act & Assert
            context.ToString().Should().Be("MyTestCase");
        }

        [Fact]
        public void Context_CallLog_ShouldBeAccessible()
        {
            // Arrange
            var context = new Context();

            // Act - Verify we can use the CallLog
            context.CallLog.AppendLine("Test message");

            // Assert
            context.CallLog.ToString().Should().Contain("Test message");
        }

        [Fact]
        public void Context_Factory_ShouldBeAccessible()
        {
            // Arrange
            var context = new Context();

            // Act - Verify we can use the Factory
            context.Factory.ClearAll(); // Should not throw

            // Assert - Factory should be working
            context.Factory.Should().NotBeNull();
        }

        [Fact]
        public void Context_MultipleInstances_ShouldHaveIndependentCallLogs()
        {
            // Arrange
            var context1 = new Context();
            var context2 = new Context();

            // Act
            context1.CallLog.AppendLine("Message 1");
            context2.CallLog.AppendLine("Message 2");

            // Assert
            context1.CallLog.ToString().Should().Contain("Message 1");
            context1.CallLog.ToString().Should().NotContain("Message 2");

            context2.CallLog.ToString().Should().Contain("Message 2");
            context2.CallLog.ToString().Should().NotContain("Message 1");
        }

        [Fact]
        public void Context_MultipleInstances_ShouldShareObjectFactory()
        {
            // Arrange
            var context1 = new Context();
            var context2 = new Context();

            // Act - Register something in one context
            var testObject = new object();
            context1.Factory.SetOne(testObject, "shared");

            // Assert - Should be accessible from other context
            var retrieved = context2.Factory.GetRegisteredObject<object>("shared");
            retrieved.Should().BeSameAs(testObject);
        }
    }
}
```

### 3.3 Test Context Class

```bash
# Run the unit tests to verify Context class works
dotnet test --filter "ContextUnitTests" --verbosity normal

# All tests should pass
```

### 3.4 Test Context Integration with Existing Patterns

Add a test to verify Context can work alongside existing CallLog patterns:

Add to `ContextUnitTests.cs`:
```csharp
[Fact]
public void Context_ShouldWorkWithExistingSpecRecPatterns()
{
    // Arrange
    var context = new Context();
    
    // Act - Use Context with existing Parrot pattern
    var service = Parrot.Create<IBasicTestService>(context.CallLog, "🔧");
    
    // Assert - Should not throw
    service.Should().NotBeNull();
    context.CallLog.ToString().Should().Contain("IBasicTestService constructor");
}

[Fact] 
public async Task Context_CallLogVerify_ShouldWork()
{
    // Arrange
    var context = new Context();
    context.CallLog.AppendLine("Test log entry");
    
    // Act & Assert - Should not throw (will create .received.txt)
    await context.CallLog.Verify();
}
```

### 3.5 Verify No Regressions

```bash
# Ensure all existing tests still pass
dotnet test --verbosity minimal

# Should show all original tests + new Context tests passing
```

### 3.6 Test Context Manual Usage

Create a simple integration test to verify Context works manually:

Add to `ContextUnitTests.cs`:
```csharp
[Fact]
public void Context_ManualUsage_ShouldSupportBasicWorkflow()
{
    // Arrange
    var context = new Context();
    
    // Act - Simulate a basic workflow
    var service = Parrot.Create<IBasicTestService>(context.CallLog, "🎯");
    context.Factory.SetOne<IBasicTestService>(service, "testService");
    
    var retrievedService = context.Factory.Create<IBasicTestService>();
    
    // Assert
    retrievedService.Should().BeSameAs(service);
    context.CallLog.ToString().Should().Contain("IBasicTestService constructor");
    
    var registeredService = context.Factory.GetRegisteredObject<IBasicTestService>("testService");
    registeredService.Should().BeSameAs(service);
}
```

## Verification Checklist

- [ ] `Context` class compiles without errors
- [ ] Default constructor creates Context with new CallLog and shared ObjectFactory
- [ ] Internal constructor accepts CallLog, ObjectFactory, and TestCaseName
- [ ] `ToString()` returns TestCaseName or "DefaultCase"
- [ ] Public properties expose CallLog and Factory
- [ ] All unit tests pass
- [ ] Context works with existing Parrot.Create pattern
- [ ] Context.CallLog.Verify() works
- [ ] Multiple Context instances have independent CallLogs
- [ ] Multiple Context instances share the same ObjectFactory
- [ ] No regressions in existing tests

## Common Issues & Solutions

**Issue**: Compilation errors with internal constructor
**Solution**: Ensure `internal` keyword is used correctly and accessing assemblies have InternalsVisibleTo if needed

**Issue**: ObjectFactory sharing issues
**Solution**: Verify ObjectFactory.Instance() returns singleton - multiple contexts should share factory

**Issue**: CallLog verification fails
**Solution**: Ensure CallLog.Verify() works independently of SpecRec discoverer

**Issue**: Unit tests fail with missing services
**Solution**: Ensure proper setup/teardown with ObjectFactory.ClearAll()

## Testing Strategy

### Unit Test Categories
1. **Constructor Tests** - Verify proper initialization
2. **Property Tests** - Verify properties return correct instances  
3. **ToString Tests** - Verify display name functionality
4. **Integration Tests** - Verify Context works with existing SpecRec components
5. **Multi-instance Tests** - Verify independence and sharing behavior

### Manual Testing
```csharp
// Simple manual test to verify Context works
var ctx = new Context();
ctx.CallLog.AppendLine("Manual test");
Console.WriteLine(ctx.ToString()); // Should print "DefaultCase"
Console.WriteLine(ctx.CallLog.ToString()); // Should contain "Manual test"
```

## Next Step
Once all verification criteria pass, proceed to [Step 4: Context Fluent API](Step4-ContextFluentAPI.md).

## Notes
- Context class should be simple and focused - no complex logic yet
- Context provides the foundation for the fluent API but doesn't implement it yet
- All existing SpecRec patterns should work unchanged
- Multiple Context instances should behave correctly in test scenarios