# Step 7: Documentation and Examples

## Objective
Create comprehensive documentation and examples that demonstrate the unified SpecRec interface, migration paths from existing patterns, and best practices for real-world usage.

## Context
With the unified interface fully implemented and tested, developers need clear guidance on how to use it effectively. This includes migration from existing patterns, best practices, and common usage examples.

## Prerequisites
- Step 6 completed successfully (end-to-end integration tests passing)
- All unified interface functionality verified
- Performance acceptable for production use
- Complete backward compatibility confirmed

## Success Criteria
- [ ] Updated README with unified interface section
- [ ] Migration guide from `[Theory][SpecRecLogs]` to `[SpecRec]`
- [ ] Comprehensive usage examples
- [ ] Best practices documentation
- [ ] Performance considerations documented
- [ ] Troubleshooting guide created
- [ ] API reference documentation
- [ ] Working examples in dedicated test file

## Implementation Steps

### 7.1 Update Main README

Update the main README.md to include unified interface documentation. Add after the existing "How It Works" section:

```markdown
## Unified Interface: Simplified SpecRec with `[SpecRec]`

**New in v2.0:** The `[SpecRec]` attribute provides a unified, fluent API that simplifies SpecRec usage while maintaining all existing functionality.

### Quick Example: Before and After

#### Before (v1.x - Still Supported)
```csharp
[Theory]
[SpecRecLogs] 
public async Task ProcessOrder(CallLog callLog, string productId = "ABC123", int quantity = 1)
{
    var orderService = Parrot.Create<IOrderService>(callLog, "🛒");
    var inventoryService = Parrot.Create<IInventoryService>(callLog, "📦");
    
    ObjectFactory.Instance().SetOne<IOrderService>(orderService, "orders");
    ObjectFactory.Instance().SetOne<IInventoryService>(inventoryService, "inventory");
    
    // Business logic...
    
    await callLog.Verify();
}
```

#### After (v2.0+)
```csharp
[SpecRec]
public async Task ProcessOrder(Context ctx, string productId = "ABC123", int quantity = 1)
{
    ctx.Substitute<IOrderService>("🛒", "orders")
       .Substitute<IInventoryService>("📦", "inventory");
    
    // Business logic...
    
    await ctx.CallLog.Verify();
}
```

### Benefits of the Unified Interface

- **Cleaner API**: Single attribute instead of `[Theory][SpecRecLogs]`
- **Fluent Methods**: Chainable methods reduce boilerplate
- **Automatic Management**: No manual ObjectFactory cleanup needed
- **Type Safety**: Same compile-time checking with better readability
- **Backward Compatible**: Existing tests continue to work unchanged

### Context Class API

The `Context` class provides these fluent methods:

#### `Substitute<T>(icon, id?)` 
Creates a parrot test double and registers it in ObjectFactory (most common pattern):
```csharp
ctx.Substitute<IOrderService>("🛒", "orderService");
```

#### `SetAlways<T>(obj, id?)` / `SetOne<T>(obj, id?)`
Registers existing objects in ObjectFactory:
```csharp
var realService = new OrderService();
ctx.SetAlways<IOrderService>(realService, "orders");
```

#### `Wrap<T>(obj, icon?)`
Wraps existing objects with call logging without registration:
```csharp
var realService = new OrderService();
var wrapped = ctx.Wrap<IOrderService>(realService, "🔍");
```

#### `CreateParrot<T>(icon?)`
Creates parrot without registration (for direct usage):
```csharp
var parrot = ctx.CreateParrot<IOrderService>("🎯");
```

### Working with Context

Access underlying components when needed:
```csharp
[SpecRec]
public async Task MyTest(Context ctx)
{
    // Access CallLog for verification
    await ctx.CallLog.Verify();
    
    // Access ObjectFactory for advanced scenarios  
    var service = ctx.Factory.Create<IOrderService>();
    
    // Test case name for conditional logic
    if (ctx.ToString() == "SpecialCase")
    {
        // Handle special test case
    }
}
```
```

### 7.2 Create Migration Guide

Create `MigrationGuide.md`:

```markdown
# Migration Guide: From [Theory][SpecRecLogs] to [SpecRec]

## Overview

The unified `[SpecRec]` interface provides a cleaner, more maintainable way to write SpecRec tests while maintaining 100% backward compatibility. This guide shows how to migrate existing tests.

## Migration Strategy

You can migrate incrementally:
1. **Coexistence**: New tests use `[SpecRec]`, existing tests remain unchanged
2. **Gradual Migration**: Migrate individual test methods as you modify them  
3. **Bulk Migration**: Convert entire test classes at once

## Step-by-Step Migration

### 1. Change the Attribute

**Before:**
```csharp
[Theory]
[SpecRecLogs]
public async Task MyTest(CallLog callLog, string param = "default")
```

**After:**
```csharp
[SpecRec]  
public async Task MyTest(Context ctx, string param = "default")
```

### 2. Replace Parrot Creation Patterns

**Before:**
```csharp
var service1 = Parrot.Create<IService1>(callLog, "🎯");
var service2 = Parrot.Create<IService2>(callLog, "🎪");

ObjectFactory.Instance().SetOne<IService1>(service1, "svc1");
ObjectFactory.Instance().SetOne<IService2>(service2, "svc2");
```

**After:**
```csharp
ctx.Substitute<IService1>("🎯", "svc1")
   .Substitute<IService2>("🎪", "svc2");
```

### 3. Replace ObjectFactory Usage

**Before:**
```csharp
ObjectFactory.Instance().SetAlways<IService>(existingService, "svc");
var service = ObjectFactory.Instance().Create<IService>();
ObjectFactory.Instance().ClearAll(); // in Dispose
```

**After:**
```csharp
ctx.SetAlways<IService>(existingService, "svc");
var service = ctx.Factory.Create<IService>();
// No manual cleanup needed
```

### 4. Replace CallLog Operations

**Before:**
```csharp
callLog.AppendLine("Custom message");
await callLog.Verify();
```

**After:**
```csharp
ctx.CallLog.AppendLine("Custom message");
await ctx.CallLog.Verify();
```

## Migration Examples

### Simple Test Migration

**Before:**
```csharp
[Theory]
[SpecRecLogs]
public async Task CalculateOrderTotal(CallLog callLog, decimal price = 19.99m, int quantity = 2)
{
    var calculator = Parrot.Create<IPriceCalculator>(callLog, "💰");
    ObjectFactory.Instance().SetOne<IPriceCalculator>(calculator);
    
    var service = new OrderService();
    var total = service.CalculateTotal("ORDER-123", price, quantity);
    
    callLog.AppendLine($"Total: ${total}");
    await callLog.Verify();
}
```

**After:**
```csharp
[SpecRec]
public async Task CalculateOrderTotal(Context ctx, decimal price = 19.99m, int quantity = 2)
{
    ctx.Substitute<IPriceCalculator>("💰");
    
    var service = new OrderService();
    var total = service.CalculateTotal("ORDER-123", price, quantity);
    
    ctx.CallLog.AppendLine($"Total: ${total}");
    await ctx.CallLog.Verify();
}
```

### Complex Test Migration

**Before:**
```csharp
[Theory]
[SpecRecLogs]
public async Task ComplexWorkflow(CallLog callLog, string customerId = "CUST001")
{
    // Multiple service setup
    var orderSvc = Parrot.Create<IOrderService>(callLog, "🛒");
    var paymentSvc = Parrot.Create<IPaymentService>(callLog, "💳");
    var inventorySvc = Parrot.Create<IInventoryService>(callLog, "📦");
    
    var factory = ObjectFactory.Instance();
    factory.SetOne<IOrderService>(orderSvc, "orders");
    factory.SetOne<IPaymentService>(paymentSvc, "payments");
    factory.SetOne<IInventoryService>(inventorySvc, "inventory");
    
    // Mixed real/mock scenario
    var realLogger = new ConsoleLogger();
    var wrappedLogger = new CallLogger(callLog.SpecBook, "📝", factory)
                          .Wrap<ILogger>(realLogger, "📝", factory);
    factory.SetOne<ILogger>(wrappedLogger, "logger");
    
    // Business logic...
    
    await callLog.Verify();
}
```

**After:**
```csharp
[SpecRec]
public async Task ComplexWorkflow(Context ctx, string customerId = "CUST001")
{
    // Clean fluent setup
    ctx.Substitute<IOrderService>("🛒", "orders")
       .Substitute<IPaymentService>("💳", "payments")  
       .Substitute<IInventoryService>("📦", "inventory");
    
    // Mixed real/mock scenario
    var realLogger = new ConsoleLogger();
    var wrappedLogger = ctx.Wrap<ILogger>(realLogger, "📝");
    ctx.SetOne<ILogger>(wrappedLogger, "logger");
    
    // Business logic...
    
    await ctx.CallLog.Verify();
}
```

## File Compatibility

- **Verified files**: No changes needed - same format and naming
- **Test discovery**: Works identically - same file patterns
- **Parameter handling**: Identical behavior with preambles and defaults

## Troubleshooting Migration

### Common Issues

1. **Compilation Error**: `The type or namespace name 'Context' could not be found`
   - **Solution**: Ensure you're using SpecRec v2.0+ with the unified interface

2. **Test Discovery Issues**: Tests not found or "No data found"
   - **Solution**: Verify `[SpecRec]` attribute is used correctly and verified files exist

3. **Context Methods Not Available**: Fluent methods missing
   - **Solution**: Check that Context parameter is first and correctly typed

4. **ObjectFactory Behavior Changes**: Different service resolution
   - **Solution**: Context uses same ObjectFactory.Instance() - behavior identical

### Validation Checklist

After migration, verify:
- [ ] All tests discover and run correctly
- [ ] Verified files load and replay properly  
- [ ] Test parameters work as before
- [ ] Performance is comparable
- [ ] No test isolation issues

## Best Practices After Migration

1. **Use Descriptive Service IDs**: `ctx.Substitute<IOrderService>("🛒", "orderService")`
2. **Chain Related Operations**: `ctx.Substitute<T1>(...).Substitute<T2>(...)`
3. **Leverage Context Properties**: Access `ctx.CallLog` and `ctx.Factory` when needed
4. **Mix Patterns When Appropriate**: Use `Wrap` for real services, `Substitute` for mocks
```

### 7.3 Create Best Practices Guide

Create `BestPractices.md`:

```markdown
# SpecRec Unified Interface Best Practices

## Context Setup Patterns

### 1. Service Identification
Use descriptive IDs and meaningful emojis:

```csharp
// Good
ctx.Substitute<IOrderService>("🛒", "orderService")
   .Substitute<IPaymentService>("💳", "paymentProcessor")
   .Substitute<IInventoryService>("📦", "stockManager");

// Avoid
ctx.Substitute<IOrderService>("🔧") // Generic emoji
   .Substitute<IPaymentService>("🔧", "svc1"); // Same emoji, unclear ID
```

### 2. Method Chaining
Group related setup operations:

```csharp
// Good - Chain related services
ctx.Substitute<IOrderService>("🛒", "orders")
   .Substitute<IInventoryService>("📦", "inventory")
   .SetAlways<ILogger>(realLogger, "logger");

// Good - Separate concerns  
ctx.Substitute<IOrderService>("🛒", "orders");
// ... business logic ...
ctx.Substitute<IPaymentService>("💳", "payments"); // When needed
```

### 3. Real vs Mock Services

Use `Wrap` for real services you want to observe:
```csharp
var realEmailService = new SmtpEmailService();
var trackedEmail = ctx.Wrap<IEmailService>(realEmailService, "📧");
ctx.SetOne<IEmailService>(trackedEmail, "emailService");
```

Use `Substitute` for pure test doubles:
```csharp
ctx.Substitute<IPaymentGateway>("💳", "mockPayments");
```

## Test Organization

### 1. Test Method Naming
```csharp
// Good - Describes scenario
[SpecRec]
public async Task ProcessOrder_WithValidCustomer_ShouldCompleteSuccessfully(Context ctx)

// Good - Business focused  
[SpecRec]
public async Task CustomerCheckout_StandardFlow(Context ctx, decimal orderTotal = 99.99m)
```

### 2. Parameter Defaults
Choose realistic defaults:
```csharp
[SpecRec]
public async Task ProcessPayment(
    Context ctx,
    string customerId = "CUST-12345",      // Realistic format
    decimal amount = 29.99m,              // Realistic amount  
    string currency = "USD",              // Common default
    bool requiresVerification = false)    // Most common case
```

### 3. Verified File Organization
Use meaningful test case names in verified files:
- `OrderTests.ProcessOrder.HappyPath.verified.txt`
- `OrderTests.ProcessOrder.InsufficientFunds.verified.txt`  
- `OrderTests.ProcessOrder.InvalidCustomer.verified.txt`

## Performance Optimization

### 1. Context Reuse
Don't create unnecessary Context instances:
```csharp
// Good - Single context per test
[SpecRec]
public async Task MyTest(Context ctx)
{
    ctx.Substitute<IService1>("🎯")
       .Substitute<IService2>("🎪");
    // Use both services...
}

// Avoid - Multiple contexts
[SpecRec] 
public async Task MyTest(Context ctx)
{
    var ctx2 = new Context(); // Usually unnecessary
}
```

### 2. Service Registration
Register services once, use multiple times:
```csharp
// Good
ctx.SetAlways<IExpensiveService>(expensiveService, "expensive");
// Service reused across multiple calls

// Avoid  
ctx.SetOne<IExpensiveService>(expensiveService1, "expensive1");
ctx.SetOne<IExpensiveService>(expensiveService2, "expensive2");
// Creating multiple instances unnecessarily
```

## Error Handling and Debugging

### 1. Descriptive Return Values
```csharp
// Good - In verified files
🛒 ProcessOrder:
  🔸 customerId: "CUST-12345" 
  🔸 items: ["PROD-123", "PROD-456"]
  🔹 Returns: "ORDER-2024-001"

// Better - With business context
🛒 ProcessOrder:
  🔸 customerId: "CUST-12345"
  🔸 items: ["Widget-Pro", "Gadget-Lite"]  
  🔹 Returns: "ORDER-NEW-CUSTOMER-001"
```

### 2. Conditional Test Logic
Use Context.ToString() for test case specific behavior:
```csharp
[SpecRec]
public async Task ProcessOrder(Context ctx, bool isPremiumCustomer = false)
{
    ctx.Substitute<IOrderService>("🛒", "orders");
    
    if (ctx.ToString() == "PremiumCustomerFlow")
    {
        ctx.Substitute<IPremiumService>("⭐", "premium");
    }
    
    // Test logic...
}
```

### 3. Debugging Support
Add context information to logs:
```csharp
ctx.CallLog.AppendLine($"Test case: {ctx.ToString()}");
ctx.CallLog.AppendLine($"Customer type: {customerType}");
// Helps identify issues in verified files
```

## Common Anti-Patterns to Avoid

### 1. Manual ObjectFactory Management
```csharp
// Avoid - Defeats purpose of unified interface
ObjectFactory.Instance().SetOne<IService>(service);
ObjectFactory.Instance().ClearAll();

// Good - Use Context
ctx.SetOne<IService>(service);
// Automatic cleanup
```

### 2. Excessive Method Chaining
```csharp
// Avoid - Hard to read
ctx.Substitute<IA>("🅰")
   .Substitute<IB>("🅱")  
   .Substitute<IC>("🅲")
   .Substitute<ID>("🅳")
   .SetAlways<IE>(e).SetOne<IF>(f)
   .Substitute<IG>("🅶");

// Good - Group logically  
ctx.Substitute<IA>("🅰")
   .Substitute<IB>("🅱")
   .Substitute<IC>("🅲");

ctx.SetAlways<IE>(realE)
   .SetOne<IF>(customF);

ctx.Substitute<IG>("🅶");
```

### 3. Ignoring Test Case Names
```csharp
// Avoid - Generic test cases
MyTest.FirstTestCase.verified.txt
MyTest.SecondTestCase.verified.txt

// Good - Descriptive names  
MyTest.ValidCustomerOrder.verified.txt
MyTest.InvalidPaymentMethod.verified.txt  
MyTest.OutOfStockScenario.verified.txt
```

## Migration Strategy

1. **Start with new tests** - Use `[SpecRec]` for all new test methods
2. **Migrate during maintenance** - Convert existing tests when modifying them
3. **Focus on complex tests first** - Biggest benefit from fluent API
4. **Keep verified files** - No changes needed to existing test data

## Performance Expectations

- Context creation: < 1ms
- Fluent API calls: < 0.1ms each
- Test discovery: Comparable to existing SpecRec
- Memory usage: Similar to `[Theory][SpecRecLogs]` pattern

## Integration with CI/CD

The unified interface works seamlessly with existing CI/CD pipelines:
- Same test discovery mechanism
- Same file naming conventions  
- Same verification workflow
- Same performance characteristics
```

### 7.4 Create API Reference

Create `UnifiedInterface-APIReference.md`:

```markdown
# Unified Interface API Reference

## SpecRecAttribute

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class SpecRecAttribute : TheoryAttribute
```

Marks a test method for SpecRec data-driven testing using the unified interface.

**Usage:**
```csharp
[SpecRec]
public async Task TestMethod(Context ctx, string param = "default") { }
```

## Context Class

The main class providing the unified fluent API for SpecRec operations.

### Constructors

```csharp
public Context()
```
Creates a new Context with a new CallLog and shared ObjectFactory.

```csharp
internal Context(CallLog callLog, ObjectFactory factory, string? testCaseName = null)
```
Internal constructor used by the discoverer to create Context with existing CallLog.

### Properties

```csharp
public CallLog CallLog { get; }
```
Access to the underlying CallLog for verification and custom logging.

```csharp
public ObjectFactory Factory { get; }
```
Access to the shared ObjectFactory for advanced scenarios.

```csharp
internal string? TestCaseName { get; set; }
```
The test case name from the verified file (internal use).

### Methods

#### Substitute<T>(string icon, string? id = null)
```csharp
public Context Substitute<T>(string icon, string? id = null) where T : class
```

Creates a parrot test double and registers it in the ObjectFactory.

**Parameters:**
- `icon`: Emoji identifier for the service in logs
- `id`: Optional registration ID (auto-generated if null)

**Returns:** Context for fluent chaining

**Example:**
```csharp
ctx.Substitute<IOrderService>("🛒", "orderService");
```

#### SetAlways<T>(T obj, string? id = null)
```csharp
public Context SetAlways<T>(T obj, string? id = null) where T : class
```

Registers an existing object to always be returned by ObjectFactory.

**Parameters:**
- `obj`: The object instance to register
- `id`: Optional registration ID

**Returns:** Context for fluent chaining

**Example:**
```csharp
var realService = new OrderService();
ctx.SetAlways<IOrderService>(realService, "orders");
```

#### SetOne<T>(T obj, string? id = null) 
```csharp
public Context SetOne<T>(T obj, string? id = null) where T : class
```

Registers an existing object to be returned once by ObjectFactory.

**Parameters:**
- `obj`: The object instance to register  
- `id`: Optional registration ID

**Returns:** Context for fluent chaining

**Example:**
```csharp
var testData = new CustomerData();
ctx.SetOne<ICustomerData>(testData, "customer");
```

#### Wrap<T>(T obj, string icon = "🔧")
```csharp
public T Wrap<T>(T obj, string icon = "🔧") where T : class
```

Wraps an existing object with call logging without registering it.

**Parameters:**
- `obj`: The object to wrap
- `icon`: Emoji identifier for logs (default: "🔧")

**Returns:** Wrapped object with call logging

**Example:**
```csharp
var realService = new EmailService();
var tracked = ctx.Wrap<IEmailService>(realService, "📧");
```

#### CreateParrot<T>(string icon = "🦜")
```csharp
public T CreateParrot<T>(string icon = "🦜") where T : class
```

Creates a parrot test double without registering it in ObjectFactory.

**Parameters:**
- `icon`: Emoji identifier for logs (default: "🦜")

**Returns:** Parrot test double

**Example:**
```csharp
var parrot = ctx.CreateParrot<INotificationService>("📨");
```

#### ToString()
```csharp
public override string ToString()
```

Returns the test case name for display in test runners.

**Returns:** Test case name or "DefaultCase" if none set

## SpecRecDiscoverer Class

```csharp
public class SpecRecDiscoverer : IDataDiscoverer
```

Handles test case discovery and Context creation for `[SpecRec]` tests.

### Constructor
```csharp
public SpecRecDiscoverer(IMessageSink diagnosticMessageSink)
```

### Methods

```csharp
public IEnumerable<object[]> GetData(IAttributeInfo dataAttribute, IMethodInfo testMethod)
```
Discovers test cases and creates Context instances.

```csharp
public bool SupportsDiscoveryEnumeration(IAttributeInfo dataAttribute, IMethodInfo testMethod)  
```
Indicates support for test enumeration.

## Usage Patterns

### Basic Pattern
```csharp
[SpecRec]
public async Task BasicTest(Context ctx, string param = "default")
{
    ctx.Substitute<IService>("🎯");
    
    // Test logic
    
    await ctx.CallLog.Verify();
}
```

### Fluent Chaining
```csharp
[SpecRec]
public async Task FluentTest(Context ctx)
{
    ctx.Substitute<IService1>("🎯")
       .Substitute<IService2>("🎪")
       .SetAlways<IService3>(realService);
    
    // Test logic
    
    await ctx.CallLog.Verify();
}
```

### Mixed Real/Mock
```csharp
[SpecRec] 
public async Task MixedTest(Context ctx)
{
    var realService = new RealService();
    var trackedService = ctx.Wrap<IService>(realService, "📊");
    
    ctx.SetOne<IService>(trackedService)
       .Substitute<IOtherService>("🎭");
    
    // Test logic
    
    await ctx.CallLog.Verify();
}
```

## File Format Compatibility

The unified interface maintains 100% compatibility with existing SpecRec file formats:

- Same `.verified.txt` and `.received.txt` naming
- Same content format and structure  
- Same parameter preamble syntax
- Same object ID resolution

## Exception Types

The unified interface uses the same exception types as existing SpecRec:

- `ParrotMissingReturnValueException`: When return values not specified
- `ParrotCallMismatchException`: When method calls don't match expectations
- `ParrotUnknownObjectException`: When object IDs cannot be resolved
```

### 7.5 Create Working Examples Collection

Create `SpecRec.Tests/UnifiedInterfaceExamples.cs`:

```csharp
using Xunit;

namespace SpecRec.Tests
{
    /// <summary>
    /// Comprehensive examples demonstrating the unified SpecRec interface.
    /// These serve as both documentation and working tests.
    /// </summary>
    public class UnifiedInterfaceExamples : IDisposable
    {
        // Example business interfaces
        public interface IShoppingCartService
        {
            void AddItem(string productId, int quantity);
            decimal GetTotal();
            string[] GetItems();
        }

        public interface IDiscountService  
        {
            decimal CalculateDiscount(decimal subtotal, string customerType);
            bool IsValidCoupon(string couponCode);
        }

        public interface IShippingService
        {
            decimal CalculateShipping(string zipCode, decimal weight);
            string[] GetAvailableMethods(string zipCode);
        }

        public UnifiedInterfaceExamples()
        {
            ObjectFactory.Instance().ClearAll();
        }

        public void Dispose()
        {
            ObjectFactory.Instance().ClearAll();
        }

        /// <summary>
        /// Example 1: Basic usage with single service
        /// </summary>
#pragma warning disable xUnit1003
        [SpecRec]
        public async Task Example1_BasicUsage(Context ctx, string productId = "WIDGET-123", int quantity = 2)
        {
            // Setup: Create a mock shopping cart service
            ctx.Substitute<IShoppingCartService>("🛒", "cart");

            // Act: Use the service
            var cart = ctx.Factory.Create<IShoppingCartService>();
            cart.AddItem(productId, quantity);
            var total = cart.GetTotal();

            ctx.CallLog.AppendLine($"Added {quantity} of {productId}, total: ${total}");

            // Verify: SpecRec handles verification automatically
            await ctx.CallLog.Verify();
        }

        /// <summary>
        /// Example 2: Multiple services with fluent chaining
        /// </summary>
        [SpecRec]
        public async Task Example2_FluentChaining(Context ctx, string customerType = "Regular")
        {
            // Setup: Chain multiple service substitutions
            ctx.Substitute<IShoppingCartService>("🛒", "cart")
               .Substitute<IDiscountService>("💰", "discounts")
               .Substitute<IShippingService>("📦", "shipping");

            // Act: Complex workflow using multiple services
            var cart = ctx.Factory.Create<IShoppingCartService>();
            var discountService = ctx.Factory.Create<IDiscountService>();
            var shippingService = ctx.Factory.Create<IShippingService>();

            cart.AddItem("PRODUCT-A", 1);
            cart.AddItem("PRODUCT-B", 2);

            var subtotal = cart.GetTotal();
            var discount = discountService.CalculateDiscount(subtotal, customerType);
            var shipping = shippingService.CalculateShipping("12345", 2.5m);

            var finalTotal = subtotal - discount + shipping;
            ctx.CallLog.AppendLine($"Final total for {customerType}: ${finalTotal}");

            await ctx.CallLog.Verify();
        }

        /// <summary>
        /// Example 3: Mixed real and mock services
        /// </summary>
        [SpecRec]
        public async Task Example3_MixedRealAndMock(Context ctx, bool useRealDiscounts = false)
        {
            // Setup: Mix real and mock services
            ctx.Substitute<IShoppingCartService>("🛒", "cart");

            if (useRealDiscounts)
            {
                var realDiscountService = new RealDiscountService();
                var wrappedDiscount = ctx.Wrap<IDiscountService>(realDiscountService, "💵");
                ctx.SetOne<IDiscountService>(wrappedDiscount, "discounts");
            }
            else
            {
                ctx.Substitute<IDiscountService>("💰", "discounts");
            }

            // Act: Business logic remains the same
            var cart = ctx.Factory.Create<IShoppingCartService>();
            var discounts = ctx.Factory.Create<IDiscountService>();

            cart.AddItem("PREMIUM-ITEM", 1);
            var subtotal = cart.GetTotal();
            var discount = discounts.CalculateDiscount(subtotal, "Premium");

            ctx.CallLog.AppendLine($"Discount calculation: ${subtotal} - ${discount} = ${subtotal - discount}");

            await ctx.CallLog.Verify();
        }

        /// <summary>
        /// Example 4: Direct parrot usage without registration
        /// </summary>
        [SpecRec]
        public async Task Example4_DirectParrotUsage(Context ctx)
        {
            // Setup: Create parrot for direct usage
            var discountService = ctx.CreateParrot<IDiscountService>("🎯");

            // Register a different service in factory  
            ctx.Substitute<IShoppingCartService>("🛒", "cart");

            // Act: Use direct parrot and factory service
            var cart = ctx.Factory.Create<IShoppingCartService>();
            cart.AddItem("ITEM-1", 3);

            var subtotal = cart.GetTotal();
            var discount = discountService.CalculateDiscount(subtotal, "VIP");

            ctx.CallLog.AppendLine($"VIP discount applied: ${discount}");

            await ctx.CallLog.Verify();
        }

        /// <summary>
        /// Example 5: Conditional logic based on test case
        /// </summary>
        [SpecRec]
        public async Task Example5_ConditionalLogic(Context ctx, string scenario = "standard")
        {
            // Setup: Different services based on test case
            ctx.Substitute<IShoppingCartService>("🛒", "cart");

            if (ctx.ToString().Contains("Premium"))
            {
                ctx.Substitute<IDiscountService>("⭐", "premiumDiscounts");
                ctx.Substitute<IShippingService>("🚀", "expressShipping");
            }
            else
            {
                ctx.Substitute<IDiscountService>("💰", "standardDiscounts");
                ctx.Substitute<IShippingService>("📦", "standardShipping");
            }

            // Act: Business logic adapts to test case
            var cart = ctx.Factory.Create<IShoppingCartService>();
            cart.AddItem("TEST-PRODUCT", 1);

            ctx.CallLog.AppendLine($"Test case: {ctx.ToString()}, Scenario: {scenario}");

            await ctx.CallLog.Verify();
        }

        /// <summary>
        /// Example 6: Complex e-commerce checkout flow
        /// </summary>
        [SpecRec]
        public async Task Example6_ComplexCheckoutFlow(
            Context ctx,
            string customerType = "Regular",
            string zipCode = "12345",
            string couponCode = "SAVE10")
        {
            // Setup: Complete checkout system
            ctx.Substitute<IShoppingCartService>("🛒", "cart")
               .Substitute<IDiscountService>("💰", "discounts")
               .Substitute<IShippingService>("📦", "shipping");

            // Act: Full checkout workflow
            var cart = ctx.Factory.Create<IShoppingCartService>();
            var discounts = ctx.Factory.Create<IDiscountService>();
            var shipping = ctx.Factory.Create<IShippingService>();

            // Add items to cart
            cart.AddItem("LAPTOP", 1);
            cart.AddItem("MOUSE", 1);
            cart.AddItem("KEYBOARD", 1);

            // Calculate pricing
            var items = cart.GetItems();
            var subtotal = cart.GetTotal();

            ctx.CallLog.AppendLine($"Cart items: {string.Join(", ", items)}");

            // Apply discounts
            var customerDiscount = discounts.CalculateDiscount(subtotal, customerType);
            var couponValid = discounts.IsValidCoupon(couponCode);
            var couponDiscount = couponValid ? 5.00m : 0m;

            // Calculate shipping
            var availableMethods = shipping.GetAvailableMethods(zipCode);
            var shippingCost = shipping.CalculateShipping(zipCode, 3.5m);

            // Final calculations
            var totalDiscount = customerDiscount + couponDiscount;
            var finalTotal = subtotal - totalDiscount + shippingCost;

            ctx.CallLog.AppendLine($"Subtotal: ${subtotal}");
            ctx.CallLog.AppendLine($"Discounts: ${totalDiscount} (Customer: ${customerDiscount}, Coupon: ${couponDiscount})");
            ctx.CallLog.AppendLine($"Shipping to {zipCode}: ${shippingCost}");
            ctx.CallLog.AppendLine($"Final total: ${finalTotal}");
            ctx.CallLog.AppendLine($"Available shipping: {string.Join(", ", availableMethods)}");

            await ctx.CallLog.Verify();
        }
#pragma warning restore xUnit1003

        // Helper class for mixed real/mock example
        private class RealDiscountService : IDiscountService
        {
            public decimal CalculateDiscount(decimal subtotal, string customerType)
            {
                return customerType switch
                {
                    "Premium" => subtotal * 0.15m,
                    "VIP" => subtotal * 0.20m,
                    _ => subtotal * 0.05m
                };
            }

            public bool IsValidCoupon(string couponCode)
            {
                return couponCode.StartsWith("SAVE");
            }
        }
    }
}
```

### 7.6 Final Documentation Review

Create `UnifiedInterface-Overview.md` as a comprehensive landing page:

```markdown
# SpecRec Unified Interface Overview

## What is the Unified Interface?

The SpecRec Unified Interface is a streamlined API introduced in v2.0 that simplifies SpecRec usage while maintaining full backward compatibility. It replaces the `[Theory][SpecRecLogs]` pattern with a single `[SpecRec]` attribute and fluent `Context` API.

## Key Benefits

### 🎯 **Simplified API**
- Single `[SpecRec]` attribute instead of `[Theory][SpecRecLogs]`
- Fluent method chaining reduces boilerplate code
- Cleaner, more readable test methods

### 🔧 **Better Developer Experience**  
- IntelliSense-friendly fluent methods
- Automatic resource management
- Clear method naming and documentation

### 🚀 **Improved Maintainability**
- Less repetitive code patterns
- Easier to refactor and modify
- Consistent API across different usage scenarios

### 🔒 **Full Compatibility**
- Existing tests continue to work unchanged
- Same file formats and naming conventions
- Same performance characteristics
- Gradual migration path

## Quick Start

### Installation
```bash
# Update to SpecRec v2.0+
dotnet add package SpecRec --version 2.0.0
```

### Basic Usage
```csharp
[SpecRec]
public async Task ProcessOrder(Context ctx, string productId = "WIDGET", int quantity = 1)
{
    // Setup services with fluent API
    ctx.Substitute<IOrderService>("🛒", "orders")
       .Substitute<IInventoryService>("📦", "inventory");
    
    // Use services normally
    var orderService = ctx.Factory.Create<IOrderService>();
    var result = orderService.ProcessOrder(productId, quantity);
    
    ctx.CallLog.AppendLine($"Processed: {result}");
    
    // Automatic verification
    await ctx.CallLog.Verify();
}
```

## Documentation Links

- **[Migration Guide](MigrationGuide.md)** - Convert existing tests
- **[Best Practices](BestPractices.md)** - Recommended patterns and anti-patterns  
- **[API Reference](UnifiedInterface-APIReference.md)** - Complete API documentation
- **[Working Examples](UnifiedInterfaceExamples.cs)** - Real-world usage examples

## Architecture

### Context Class
Central to the unified interface, providing:
- **Fluent API**: Chainable methods for service setup
- **Service Management**: Integration with ObjectFactory and CallLogger
- **Test Integration**: Seamless xUnit test discovery and execution

### Discovery System
- **SpecRecAttribute**: Marks test methods for unified interface
- **SpecRecDiscoverer**: Handles test case discovery and Context creation
- **File Compatibility**: Works with existing verified files

## Common Usage Patterns

### Service Substitution (Most Common)
```csharp
ctx.Substitute<IService>("🎯", "serviceId");
```

### Real Service Wrapping  
```csharp
var real = new RealService();
var wrapped = ctx.Wrap<IService>(real, "📊");
```

### Mixed Scenarios
```csharp
ctx.Substitute<IMockService>("🎭")
   .SetAlways<IRealService>(realInstance)
   .Wrap<ITrackedService>(trackedInstance, "📈");
```

## Migration Strategy

1. **New Tests**: Use `[SpecRec]` for all new test methods
2. **Incremental**: Convert existing tests during maintenance
3. **Coexistence**: Both patterns work side-by-side
4. **No Breaking Changes**: Existing tests continue to work

## Support and Feedback

- **Issues**: Report problems on GitHub Issues
- **Questions**: Ask on project discussions
- **Contributions**: Pull requests welcome

The unified interface represents the future of SpecRec testing while preserving all existing functionality. Start using it in new tests today!
```

### 7.7 Final Verification

```bash
# Test that all examples compile and work
dotnet build

# Run example tests (they should create appropriate .received.txt files)
dotnet test --filter "UnifiedInterfaceExamples" --verbosity normal

# Verify all documentation links work and files exist
ls -la *Guide.md *Reference.md *Examples.cs
```

## Verification Checklist

### Documentation Completeness
- [ ] Main README updated with unified interface section
- [ ] Complete migration guide with step-by-step examples  
- [ ] Best practices with do's and don'ts
- [ ] Comprehensive API reference
- [ ] Working examples that compile and run
- [ ] Overview document tying everything together

### Content Quality
- [ ] All code examples compile and work
- [ ] Migration examples show realistic before/after
- [ ] Best practices cover common scenarios and anti-patterns
- [ ] API reference is accurate and complete
- [ ] Examples demonstrate real-world complexity

### User Experience
- [ ] Clear learning path from basic to advanced usage
- [ ] Troubleshooting guidance for common issues
- [ ] Performance expectations clearly documented
- [ ] Migration strategy accommodates different team needs

## Success Metrics

After completing this step, you should have:
- Complete documentation for the unified interface
- Clear migration path for existing users  
- Working examples demonstrating all features
- Performance benchmarks and expectations
- Troubleshooting guides for common issues

## Notes
- Documentation should be kept up-to-date as the interface evolves
- Examples should be tested regularly to ensure they remain accurate
- Consider creating video tutorials or blog posts for complex scenarios
- Gather user feedback to improve documentation clarity and completeness

This completes the comprehensive step-by-step implementation plan for the unified SpecRec interface. Each step builds incrementally on the previous one, ensuring a solid foundation and verifiable progress toward the final goal.