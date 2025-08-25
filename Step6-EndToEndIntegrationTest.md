# Step 6: End-to-End Integration Test (Verify Complete Workflow)

## Objective
Create comprehensive end-to-end tests that demonstrate the complete unified SpecRec interface workflow from initial test creation through the full TDD cycle with multiple test cases.

## Context
This step validates that the entire unified interface works as intended for real-world usage scenarios. It should demonstrate the complete value proposition: cleaner API, automatic management, and seamless TDD workflow.

## Prerequisites
- Step 5 completed successfully (Context discoverer transformation works)
- All previous integration tests passing
- Understanding of complete SpecRec TDD workflow

## Success Criteria
- [ ] Complete TDD workflow: red → green → multiple test cases
- [ ] `.received.txt` files created correctly for new tests
- [ ] `.verified.txt` files processed correctly with return values
- [ ] Multiple test cases work with different parameters and preambles
- [ ] Complex business scenarios work end-to-end
- [ ] Performance is acceptable for real usage
- [ ] No regressions in existing functionality
- [ ] Clear demonstration of unified interface benefits

## Implementation Steps

### 6.1 Create Comprehensive Business Scenario Test

Create `SpecRec.Tests/EndToEndWorkflowTests.cs`:
```csharp
using Xunit;
using FluentAssertions;

namespace SpecRec.Tests
{
    // Business interfaces for realistic scenario
    public interface IOrderService
    {
        string CreateOrder(string customerId, string[] productIds, int[] quantities);
        bool ValidateOrder(string orderId);
        decimal CalculateOrderTotal(string orderId);
    }

    public interface IInventoryService
    {
        bool CheckStock(string productId, int quantity);
        void ReserveItems(string productId, int quantity);
        void ReleaseReservation(string reservationId);
    }

    public interface IPaymentService
    {
        string ProcessPayment(string customerId, decimal amount, string paymentMethod);
        bool RefundPayment(string paymentId, decimal amount);
    }

    public interface INotificationService
    {
        void SendOrderConfirmation(string customerId, string orderId);
        void SendPaymentConfirmation(string customerId, string paymentId);
        void SendInventoryAlert(string productId, int stockLevel);
    }

    public class EndToEndWorkflowTests : IDisposable
    {
        public EndToEndWorkflowTests()
        {
            ObjectFactory.Instance().ClearAll();
        }

        public void Dispose()
        {
            ObjectFactory.Instance().ClearAll();
        }

        /// <summary>
        /// Complete e-commerce order workflow demonstrating unified interface
        /// </summary>
#pragma warning disable xUnit1003 // Theory methods must have test data - SpecRec provides data via discoverer
        [SpecRec]
        public async Task CompleteOrderWorkflow(
            Context ctx,
            string customerId = "CUST001",
            string productId = "PROD123",
            int quantity = 2,
            decimal unitPrice = 29.99m,
            string paymentMethod = "CreditCard")
        {
            // Arrange - Use unified Context API to set up all services
            ctx.Substitute<IOrderService>("🛒", "orderService")
               .Substitute<IInventoryService>("📦", "inventoryService")
               .Substitute<IPaymentService>("💳", "paymentService")
               .Substitute<INotificationService>("📧", "notificationService");

            // Act - Execute complete business workflow
            var orderService = ctx.Factory.Create<IOrderService>();
            var inventoryService = ctx.Factory.Create<IInventoryService>();
            var paymentService = ctx.Factory.Create<IPaymentService>();
            var notificationService = ctx.Factory.Create<INotificationService>();

            // Step 1: Check inventory
            var stockAvailable = inventoryService.CheckStock(productId, quantity);
            ctx.CallLog.AppendLine($"Stock check for {productId} (qty: {quantity}): {stockAvailable}");

            if (stockAvailable)
            {
                // Step 2: Reserve items
                inventoryService.ReserveItems(productId, quantity);
                ctx.CallLog.AppendLine($"Reserved {quantity} units of {productId}");

                // Step 3: Create order
                var orderId = orderService.CreateOrder(customerId, new[] { productId }, new[] { quantity });
                ctx.CallLog.AppendLine($"Created order: {orderId}");

                // Step 4: Validate order
                var orderValid = orderService.ValidateOrder(orderId);
                ctx.CallLog.AppendLine($"Order validation: {orderValid}");

                if (orderValid)
                {
                    // Step 5: Calculate total
                    var totalAmount = orderService.CalculateOrderTotal(orderId);
                    ctx.CallLog.AppendLine($"Order total: ${totalAmount}");

                    // Step 6: Process payment
                    var paymentId = paymentService.ProcessPayment(customerId, totalAmount, paymentMethod);
                    ctx.CallLog.AppendLine($"Payment processed: {paymentId}");

                    // Step 7: Send confirmations
                    notificationService.SendOrderConfirmation(customerId, orderId);
                    notificationService.SendPaymentConfirmation(customerId, paymentId);
                    
                    ctx.CallLog.AppendLine($"Workflow completed successfully for customer {customerId}");
                }
            }

            // Assert - Verify through CallLog
            await ctx.CallLog.Verify();
        }
#pragma warning restore xUnit1003
    }
}
```

### 6.2 Execute Complete TDD Cycle

#### 6.2.1 Red Phase - Run Initial Test

```bash
# Run the test - should fail with missing return values
dotnet test --filter "CompleteOrderWorkflow" --verbosity normal
```

**Expected Behavior:**
- Test fails with `ParrotMissingReturnValueException`
- Creates `EndToEndWorkflowTests.CompleteOrderWorkflow.FirstTestCase.received.txt`
- File shows first missing return value

**Expected File Content (first iteration):**
```
📦 CheckStock:
  🔸 productId: "PROD123"
  🔸 quantity: 2
  🔹 Returns: <missing_value>

Stock check for PROD123 (qty: 2): <missing_value>
```

#### 6.2.2 Green Phase - Iterative Return Value Completion

Create `EndToEndWorkflowTests.CompleteOrderWorkflow.FirstTestCase.verified.txt`:

**First iteration - Add CheckStock return:**
```
📦 CheckStock:
  🔸 productId: "PROD123"
  🔸 quantity: 2
  🔹 Returns: true

Stock check for PROD123 (qty: 2): True
```

Run test again:
```bash
dotnet test --filter "CompleteOrderWorkflow" --verbosity normal
```

**Continue iteratively until complete verified file:**
```
📦 CheckStock:
  🔸 productId: "PROD123"
  🔸 quantity: 2
  🔹 Returns: true

Stock check for PROD123 (qty: 2): True

📦 ReserveItems:
  🔸 productId: "PROD123"
  🔸 quantity: 2

Reserved 2 units of PROD123

🛒 CreateOrder:
  🔸 customerId: "CUST001"
  🔸 productIds: ["PROD123"]
  🔸 quantities: [2]
  🔹 Returns: "ORD-2024-001"

Created order: ORD-2024-001

🛒 ValidateOrder:
  🔸 orderId: "ORD-2024-001"
  🔹 Returns: true

Order validation: True

🛒 CalculateOrderTotal:
  🔸 orderId: "ORD-2024-001"
  🔹 Returns: 59.98

Order total: $59.98

💳 ProcessPayment:
  🔸 customerId: "CUST001"
  🔸 amount: 59.98
  🔸 paymentMethod: "CreditCard"
  🔹 Returns: "PAY-2024-001"

Payment processed: PAY-2024-001

📧 SendOrderConfirmation:
  🔸 customerId: "CUST001"
  🔸 orderId: "ORD-2024-001"

📧 SendPaymentConfirmation:
  🔸 customerId: "CUST001"
  🔸 paymentId: "PAY-2024-001"

Workflow completed successfully for customer CUST001
```

#### 6.2.3 Verify Complete Test Passes

```bash
# Test should now pass completely
dotnet test --filter "CompleteOrderWorkflow" --verbosity normal
```

### 6.3 Add Multiple Test Scenarios

#### 6.3.1 Premium Customer Scenario

Create `EndToEndWorkflowTests.CompleteOrderWorkflow.PremiumCustomer.verified.txt`:
```
📋 <Test Inputs>
  🔸 customerId: "CUST-PREMIUM-001"
  🔸 productId: "PROD-PREMIUM-999"
  🔸 quantity: 5
  🔸 unitPrice: 199.99
  🔸 paymentMethod: "PlatinumCard"

📦 CheckStock:
  🔸 productId: "PROD-PREMIUM-999"
  🔸 quantity: 5
  🔹 Returns: true

Stock check for PROD-PREMIUM-999 (qty: 5): True

📦 ReserveItems:
  🔸 productId: "PROD-PREMIUM-999"
  🔸 quantity: 5

Reserved 5 units of PROD-PREMIUM-999

🛒 CreateOrder:
  🔸 customerId: "CUST-PREMIUM-001"
  🔸 productIds: ["PROD-PREMIUM-999"]
  🔸 quantities: [5]
  🔹 Returns: "ORD-PREMIUM-2024-001"

Created order: ORD-PREMIUM-2024-001

🛒 ValidateOrder:
  🔸 orderId: "ORD-PREMIUM-2024-001"
  🔹 Returns: true

Order validation: True

🛒 CalculateOrderTotal:
  🔸 orderId: "ORD-PREMIUM-2024-001"
  🔹 Returns: 999.95

Order total: $999.95

💳 ProcessPayment:
  🔸 customerId: "CUST-PREMIUM-001"
  🔸 amount: 999.95
  🔸 paymentMethod: "PlatinumCard"
  🔹 Returns: "PAY-PREMIUM-2024-001"

Payment processed: PAY-PREMIUM-2024-001

📧 SendOrderConfirmation:
  🔸 customerId: "CUST-PREMIUM-001"
  🔸 orderId: "ORD-PREMIUM-2024-001"

📧 SendPaymentConfirmation:
  🔸 customerId: "CUST-PREMIUM-001"
  🔸 paymentId: "PAY-PREMIUM-2024-001"

Workflow completed successfully for customer CUST-PREMIUM-001
```

#### 6.3.2 Out of Stock Scenario

Create `EndToEndWorkflowTests.CompleteOrderWorkflow.OutOfStock.verified.txt`:
```
📋 <Test Inputs>
  🔸 customerId: "CUST002"
  🔸 productId: "PROD-LIMITED"
  🔸 quantity: 10
  🔸 unitPrice: 49.99
  🔸 paymentMethod: "DebitCard"

📦 CheckStock:
  🔸 productId: "PROD-LIMITED"
  🔸 quantity: 10
  🔹 Returns: false

Stock check for PROD-LIMITED (qty: 10): False
```

### 6.4 Test Multiple Scenarios

```bash
# Should run all three scenarios
dotnet test --filter "CompleteOrderWorkflow" --verbosity normal
```

**Expected Output:**
```
✓ CompleteOrderWorkflow(ctx: FirstTestCase, customerId: "CUST001", ...)
✓ CompleteOrderWorkflow(ctx: PremiumCustomer, customerId: "CUST-PREMIUM-001", ...)  
✓ CompleteOrderWorkflow(ctx: OutOfStock, customerId: "CUST002", ...)
```

### 6.5 Add Advanced Workflow Test

Create a test that demonstrates advanced Context features:

```csharp
[SpecRec]
public async Task AdvancedContextFeatures(Context ctx, bool useRealService = false)
{
    if (useRealService)
    {
        // Mix real and mock services
        var realInventory = new RealInventoryService(); // Would need implementation
        var wrappedInventory = ctx.Wrap<IInventoryService>(realInventory, "📊");
        ctx.SetOne<IInventoryService>(wrappedInventory, "inventory");
    }
    else
    {
        // Use pure mock
        ctx.Substitute<IInventoryService>("📦", "inventory");
    }

    // Add additional services
    ctx.Substitute<IOrderService>("🛒", "orders")
       .CreateParrot<INotificationService>("📨"); // Not registered, used directly

    var inventory = ctx.Factory.GetRegisteredObject<IInventoryService>("inventory");
    var orders = ctx.Factory.GetRegisteredObject<IOrderService>("orders");
    var notifications = ctx.CreateParrot<INotificationService>("📧");

    // Execute mixed workflow
    var hasStock = inventory.CheckStock("ADVANCED-PROD", 3);
    if (hasStock)
    {
        var orderId = orders.CreateOrder("ADV-CUSTOMER", ["ADVANCED-PROD"], [3]);
        notifications.SendOrderConfirmation("ADV-CUSTOMER", orderId);
        ctx.CallLog.AppendLine($"Advanced workflow completed: {orderId}");
    }

    await ctx.CallLog.Verify();
}

// Placeholder for real service (would be implemented in real scenario)
private class RealInventoryService : IInventoryService
{
    public bool CheckStock(string productId, int quantity) => quantity <= 5;
    public void ReserveItems(string productId, int quantity) { }
    public void ReleaseReservation(string reservationId) { }
}
```

### 6.6 Performance and Scale Testing

Add performance verification test:

```csharp
[Fact]
public void PerformanceTest_LargeScaleContextUsage()
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    // Create many contexts to test performance
    for (int i = 0; i < 100; i++)
    {
        using var ctx = new Context();
        
        // Use fluent API extensively
        ctx.Substitute<IOrderService>("🛒", $"order_{i}")
           .Substitute<IInventoryService>("📦", $"inventory_{i}")
           .SetOne(new RealInventoryService(), $"real_{i}")
           .Wrap(new RealInventoryService(), "📊");
    }
    
    stopwatch.Stop();
    
    // Performance should be reasonable (adjust threshold as needed)
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, 
        "Context creation and fluent API should be fast");
    
    Console.WriteLine($"Created 100 contexts with fluent API in {stopwatch.ElapsedMilliseconds}ms");
}
```

### 6.7 Integration with Existing Patterns

Verify new unified interface works alongside existing patterns:

```csharp
[Fact]
public void BackwardCompatibility_UnifiedAndLegacyPatterns()
{
    // Test that unified interface doesn't break existing patterns
    using var ctx = new Context();
    
    // Use new unified pattern
    ctx.Substitute<IOrderService>("🛒", "newStyle");
    
    // Use legacy patterns
    var legacyService = Parrot.Create<IInventoryService>(ctx.CallLog, "📦");
    ctx.Factory.SetOne(legacyService, "legacyStyle");
    
    // Both should work
    var newService = ctx.Factory.GetRegisteredObject<IOrderService>("newStyle");
    var oldService = ctx.Factory.GetRegisteredObject<IInventoryService>("legacyStyle");
    
    newService.Should().NotBeNull();
    oldService.Should().NotBeNull();
    oldService.Should().BeSameAs(legacyService);
}
```

### 6.8 Final Verification

```bash
# Run all end-to-end tests
dotnet test --filter "EndToEndWorkflowTests" --verbosity normal

# Run complete test suite to ensure no regressions
dotnet test --verbosity minimal

# Should show all tests passing with good performance
```

## Verification Checklist

### Complete TDD Workflow
- [ ] Initial test run creates `.received.txt` with missing values
- [ ] Iterative completion of return values works smoothly
- [ ] Final test passes with complete `.verified.txt`
- [ ] Multiple test cases work with different parameters

### Business Scenario Coverage
- [ ] Complex multi-service workflow works end-to-end
- [ ] Different business scenarios (happy path, edge cases) work
- [ ] Parameter customization works correctly
- [ ] Realistic business logic patterns supported

### Advanced Features  
- [ ] Mixed real/mock services work
- [ ] Context fluent API works in complex scenarios
- [ ] Performance is acceptable for real usage
- [ ] Memory usage is reasonable

### Compatibility
- [ ] Unified interface works alongside existing patterns
- [ ] No regressions in existing functionality
- [ ] File formats remain compatible
- [ ] Discovery mechanism works reliably

## Common Issues & Solutions

**Issue**: Tests are slow to complete TDD cycle
**Solution**: Optimize Parrot creation, consider return value suggestions

**Issue**: Complex scenarios create huge verified files  
**Solution**: Break down into smaller, focused test methods

**Issue**: Parameter parsing fails with complex types
**Solution**: Verify ValueParser handles all required types correctly

**Issue**: Memory usage grows with many Context instances
**Solution**: Ensure proper disposal and ObjectFactory cleanup

## Performance Expectations

- Context creation: < 1ms per instance
- Fluent API methods: < 0.1ms per call  
- Test discovery: < 100ms for typical project
- Full test execution: Comparable to existing SpecRec performance

## Next Step
Once all verification criteria pass, proceed to [Step 7: Documentation and Examples](Step7-DocumentationAndExamples.md).

## Notes
- This step validates the complete value proposition of the unified interface
- Real-world complexity should be handled gracefully
- Performance should not be significantly worse than existing patterns
- The TDD workflow should feel smooth and natural for developers
- Consider creating additional business scenarios relevant to your domain