# Step 5: Update Discoverer for Context (Verify Transformation)

## Objective
Modify the `SpecRecDiscoverer` to convert `CallLog` test data to `Context` test data, enabling `[SpecRec]` tests to use `Context` as the first parameter instead of `CallLog`.

## Context
This step bridges the existing SpecRec infrastructure (which works with `CallLog`) with the new unified interface (which uses `Context`). The discoverer must transform test data while preserving all existing functionality.

## Prerequisites
- Step 4 completed successfully (Context class with fluent API works)
- All Context unit tests passing
- Basic `SpecRecDiscoverer` working with `CallLog` parameter

## Success Criteria
- [ ] `SpecRecDiscoverer` transforms `CallLog` to `Context` in test data
- [ ] `[SpecRec]` test with `Context` parameter works correctly
- [ ] Context receives correct `TestCaseName` from verified file
- [ ] Context receives loaded `CallLog` with verified data
- [ ] Context receives proper `ObjectFactory` instance
- [ ] Parameter parsing for additional test method parameters works
- [ ] Multiple test cases work correctly
- [ ] All existing functionality preserved

## Implementation Steps

### 5.1 Update SpecRecDiscoverer for Context Transformation

Update `SpecRec/SpecRecDiscoverer.cs`:
```csharp
using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SpecRec
{
    public class SpecRecDiscoverer : IDataDiscoverer
    {
        private readonly SpecRecLogsDiscoverer _baseDiscoverer;

        public SpecRecDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _baseDiscoverer = new SpecRecLogsDiscoverer(diagnosticMessageSink);
        }

        public IEnumerable<object[]> GetData(IAttributeInfo dataAttribute, IMethodInfo testMethod)
        {
            // Get test data from base discoverer (CallLog + parameters)
            var callLogTestData = _baseDiscoverer.GetData(dataAttribute, testMethod);
            
            // Transform CallLog test cases to Context test cases
            return callLogTestData.Select(testCase => ConvertToContextTestCase(testCase));
        }

        public bool SupportsDiscoveryEnumeration(IAttributeInfo dataAttribute, IMethodInfo testMethod)
        {
            return _baseDiscoverer.SupportsDiscoveryEnumeration(dataAttribute, testMethod);
        }

        private object[] ConvertToContextTestCase(object[] callLogTestCase)
        {
            // First parameter should be CallLog from base discoverer
            if (callLogTestCase.Length > 0 && callLogTestCase[0] is CallLog callLog)
            {
                // Create Context from CallLog
                var context = CreateContextFromCallLog(callLog);
                
                // Replace CallLog with Context in test case data
                var contextTestCase = new object[callLogTestCase.Length];
                contextTestCase[0] = context;
                
                // Copy remaining parameters unchanged
                for (int i = 1; i < callLogTestCase.Length; i++)
                {
                    contextTestCase[i] = callLogTestCase[i];
                }
                
                return contextTestCase;
            }
            
            // Fallback: return original test case if something unexpected happens
            return callLogTestCase;
        }

        private Context CreateContextFromCallLog(CallLog callLog)
        {
            // Use internal constructor to preserve CallLog state
            var context = new Context(callLog, ObjectFactory.Instance(), callLog.TestCaseName);
            return context;
        }
    }
}
```

### 5.2 Create Context Integration Test

Create `SpecRec.Tests/ContextIntegrationTests.cs`:
```csharp
using Xunit;
using FluentAssertions;

namespace SpecRec.Tests
{
    public interface IContextIntegrationService
    {
        string ProcessOrder(string orderId, int quantity);
        decimal CalculateTotal(decimal price, int quantity);
    }

    public class ContextIntegrationTests : IDisposable
    {
        public ContextIntegrationTests()
        {
            ObjectFactory.Instance().ClearAll();
        }

        public void Dispose()
        {
            ObjectFactory.Instance().ClearAll();
        }

        // Test that Context parameter works with SpecRec discoverer
#pragma warning disable xUnit1003 // Theory methods must have test data - SpecRec provides data via discoverer
        [SpecRec]
        public async Task ContextBasicTest(Context ctx, string productId = "PROD123", int quantity = 2)
        {
            // Arrange - Use Context fluent API
            ctx.Substitute<IContextIntegrationService>("🛒", "orderService");

            // Act - Use the registered service
            var service = ctx.Factory.Create<IContextIntegrationService>();
            var orderId = $"ORDER-{productId}-{quantity}";
            var result = service.ProcessOrder(orderId, quantity);

            // Log the result
            ctx.CallLog.AppendLine($"Order processed: {result}");

            // Assert - Verify through CallLog
            await ctx.CallLog.Verify();
        }
#pragma warning restore xUnit1003
    }
}
```

### 5.3 Test Basic Context Integration

#### 5.3.1 Run Initial Test (Should Create .received.txt)

```bash
# Run the test - should fail with missing return values
dotnet test --filter "ContextBasicTest" --verbosity normal
```

**Expected Behavior:**
- Test runs but fails with `ParrotMissingReturnValueException`
- Creates file: `ContextIntegrationTests.ContextBasicTest.FirstTestCase.received.txt`
- File should contain parrot calls with `<missing_value>` placeholders

**Expected File Content:**
```
🛒 ProcessOrder:
  🔸 orderId: "ORDER-PROD123-2"
  🔸 quantity: 2
  🔹 Returns: <missing_value>

Order processed: <missing_value>
```

#### 5.3.2 Create Verified File

Copy `.received.txt` to `.verified.txt` and fill in return values:

`ContextIntegrationTests.ContextBasicTest.FirstTestCase.verified.txt`:
```
🛒 ProcessOrder:
  🔸 orderId: "ORDER-PROD123-2"
  🔸 quantity: 2
  🔹 Returns: "Order ORDER-PROD123-2 processed successfully"

Order processed: Order ORDER-PROD123-2 processed successfully
```

#### 5.3.3 Verify Test Passes

```bash
# Test should now pass
dotnet test --filter "ContextBasicTest" --verbosity normal
```

### 5.4 Test Context with Custom Parameters

#### 5.4.1 Create Custom Test Case

Create `ContextIntegrationTests.ContextBasicTest.LargeOrder.verified.txt`:
```
📋 <Test Inputs>
  🔸 productId: "PROD999"
  🔸 quantity: 50

🛒 ProcessOrder:
  🔸 orderId: "ORDER-PROD999-50"
  🔸 quantity: 50
  🔹 Returns: "Bulk order ORDER-PROD999-50 processed"

Order processed: Bulk order ORDER-PROD999-50 processed
```

#### 5.4.2 Run Multiple Test Cases

```bash
# Should run both FirstTestCase and LargeOrder
dotnet test --filter "ContextBasicTest" --verbosity normal
```

**Expected Output:**
```
✓ ContextBasicTest(ctx: FirstTestCase, productId: "PROD123", quantity: 2)
✓ ContextBasicTest(ctx: LargeOrder, productId: "PROD999", quantity: 50)
```

### 5.5 Test Context Properties in Test

Add verification test to ensure Context properties work correctly:

Add to `ContextIntegrationTests.cs`:
```csharp
[SpecRec]
public async Task ContextPropertiesTest(Context ctx, string testValue = "TestData")
{
    // Verify Context properties are correctly set
    ctx.Should().NotBeNull();
    ctx.CallLog.Should().NotBeNull();
    ctx.Factory.Should().NotBeNull();
    
    // TestCaseName should be set by discoverer
    ctx.ToString().Should().NotBe("DefaultCase"); // Should have actual test case name
    
    // Verify Context works with fluent API
    ctx.Substitute<IContextIntegrationService>("🔍", "testService");
    
    var service = ctx.Factory.Create<IContextIntegrationService>();
    var result = service.ProcessOrder(testValue, 1);
    
    ctx.CallLog.AppendLine($"Context test result: {result}");
    
    // Verify CallLog functionality
    ctx.CallLog.ToString().Should().Contain("ProcessOrder");
    ctx.CallLog.ToString().Should().Contain(testValue);
    
    await ctx.CallLog.Verify();
}
```

Create verified file `ContextIntegrationTests.ContextPropertiesTest.FirstTestCase.verified.txt`:
```
🔍 ProcessOrder:
  🔸 orderId: "TestData"
  🔸 quantity: 1
  🔹 Returns: "Test result for TestData"

Context test result: Test result for TestData
```

### 5.6 Test Context Fluent API Integration

Add test that uses multiple fluent API methods:

```csharp
[SpecRec]
public async Task ContextFluentAPITest(Context ctx)
{
    // Use multiple fluent API methods
    ctx.Substitute<IContextIntegrationService>("🛒", "orderService")
       .Substitute<IContextIntegrationService>("💰", "paymentService");
    
    var orderService = ctx.Factory.GetRegisteredObject<IContextIntegrationService>("orderService");
    var paymentService = ctx.Factory.GetRegisteredObject<IContextIntegrationService>("paymentService");
    
    // Verify both services exist and are different
    orderService.Should().NotBeNull();
    paymentService.Should().NotBeNull();
    orderService.Should().NotBeSameAs(paymentService);
    
    // Use the services
    var orderResult = orderService.ProcessOrder("ORDER-001", 5);
    var paymentResult = paymentService.CalculateTotal(19.99m, 5);
    
    ctx.CallLog.AppendLine($"Order: {orderResult}, Total: {paymentResult}");
    
    await ctx.CallLog.Verify();
}
```

Create verified file:
```
🛒 ProcessOrder:
  🔸 orderId: "ORDER-001"
  🔸 quantity: 5
  🔹 Returns: "Order ORDER-001 completed"

💰 CalculateTotal:
  🔸 price: 19.99
  🔸 quantity: 5
  🔹 Returns: 99.95

Order: Order ORDER-001 completed, Total: 99.95
```

### 5.7 Verify All Integration Tests

```bash
# Run all Context integration tests
dotnet test --filter "ContextIntegrationTests" --verbosity normal

# Run all tests to ensure no regressions
dotnet test --verbosity minimal
```

## Verification Checklist

### Discoverer Functionality
- [ ] `SpecRecDiscoverer` successfully converts `CallLog` to `Context`
- [ ] Context receives correct `TestCaseName` from verified file
- [ ] Context receives loaded `CallLog` with all verified data
- [ ] Context uses shared `ObjectFactory` instance
- [ ] Additional method parameters parsed correctly

### Context Integration  
- [ ] `[SpecRec]` test with `Context` parameter runs successfully
- [ ] Context properties accessible in test methods
- [ ] Context fluent API methods work in test context
- [ ] Multiple test cases work with different parameter values

### File Management
- [ ] `.received.txt` files created correctly for new tests
- [ ] `.verified.txt` files loaded and processed correctly
- [ ] Multiple test cases discovered from different verified files
- [ ] File naming convention preserved

### Compatibility
- [ ] All existing `[Theory][SpecRecLogs]` tests still work
- [ ] No regressions in existing SpecRec functionality
- [ ] Performance acceptable (no significant slowdown)

## Common Issues & Solutions

**Issue**: "No data found for test method"  
**Solution**: Verify `SpecRecLogsDiscoverer` is returning data - add debugging to check base discoverer

**Issue**: Context has wrong TestCaseName
**Solution**: Check `CallLog.TestCaseName` is set correctly by base discoverer

**Issue**: Context CallLog is empty
**Solution**: Verify `CallLog` state is preserved during Context creation

**Issue**: ObjectFactory not shared correctly
**Solution**: Ensure `ObjectFactory.Instance()` returns same instance across Context creation

**Issue**: Test parameters not parsed
**Solution**: Verify parameter transformation preserves additional test method parameters

## Debugging Tips

### Add Diagnostic Logging
```csharp
private object[] ConvertToContextTestCase(object[] callLogTestCase)
{
    Console.WriteLine($"Converting test case with {callLogTestCase.Length} parameters");
    
    if (callLogTestCase[0] is CallLog callLog)
    {
        Console.WriteLine($"CallLog TestCaseName: {callLog.TestCaseName}");
        Console.WriteLine($"CallLog content length: {callLog.ToString().Length}");
        
        var context = CreateContextFromCallLog(callLog);
        Console.WriteLine($"Created Context with TestCaseName: {context.ToString()}");
        
        // ... rest of method
    }
    
    return contextTestCase;
}
```

### Test Manual Context Creation
```csharp
[Fact]
public void ManualContextCreation_ShouldMatchDiscovererBehavior()
{
    // Create CallLog manually like discoverer would
    var callLog = new CallLog();
    callLog.TestCaseName = "ManualTest";
    
    // Create Context like discoverer does
    var context = new Context(callLog, ObjectFactory.Instance(), "ManualTest");
    
    // Verify properties
    context.ToString().Should().Be("ManualTest");
    context.CallLog.Should().BeSameAs(callLog);
}
```

## Next Step
Once all verification criteria pass, proceed to [Step 6: End-to-End Integration Test](Step6-EndToEndIntegrationTest.md).

## Notes
- The discoverer transformation should be transparent to test methods
- Context should behave identically to manually created Context instances
- All existing CallLog functionality should work through Context.CallLog
- ObjectFactory sharing is critical for proper test isolation