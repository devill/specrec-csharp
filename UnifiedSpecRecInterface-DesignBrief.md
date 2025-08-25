# Unified SpecRec Interface - Design Brief

## Overview

This document outlines the design for a unified `[SpecRec]` test interface that simplifies SpecRec test creation by providing a single, consistent API for all SpecRec operations while maintaining type safety and clean test display names.

## Goals

- **Unified API**: Single attribute and context class for all SpecRec operations
- **Clean Test Names**: First parameter shows descriptive test case name (e.g., `ctx: EventCreation`)
- **Type Safety**: Compile-time verification of test inputs with visible defaults
- **Implicit Management**: Automatic CallLog verification and ObjectFactory cleanup
- **Feature Complete**: Support all existing SpecRec capabilities
- **Backward Compatible**: Existing `[Theory][SpecRecLogs]` tests continue to work

## Core Interface

### Context Class
```csharp
public class Context
{
    // Substitute - creates a parrot and registers in factory
    public Context Substitute<T>(char icon, string? id = null) where T : class;

    // ObjectFactory operations - register existing objects
    public Context SetAlways<T>(T obj, string? id = null) where T : class;
    public Context SetOne<T>(T obj, string? id = null) where T : class;

    // CallLogger operations - work with existing objects
    public T Wrap<T>(T obj, char? icon = '🔧') where T : class;         // Wraps but doesn't register
    public T CreateParrot<T>(char? icon = '🦜') where T : class;        // Creates but doesn't register

    // Display name for test parameters
    public override string ToString() => TestCaseName ?? "DefaultCase";

    // Internal properties (framework use)
    internal string? TestCaseName { get; set; }
    internal CallLog CallLog { get; set; }
    internal ObjectFactory Factory { get; set; }
}
```

### SpecRec Attribute
```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class SpecRecAttribute : TheoryAttribute
{
    // Uses custom discoverer similar to SpecRecLogsDiscoverer
}
```

## Usage Examples

### Basic Substitute Pattern
```csharp
[SpecRec]
public async Task BookFlight(Context ctx, int passengerCount, string airlineCode = "UA") 
{
    ctx.Substitute<IBookingRepository>('💾')
       .Substitute<IFlightService>('✈️', "flightServiceId");

    var coordinator = new BookingCoordinator();
    return coordinator.BookFlight(passengerCount, airlineCode);
}
```
**Test Name:** `BookFlight(ctx: EventCreation, passengerCount: 2, airlineCode: "AA")`

### Object Registration
```csharp
[SpecRec]
public async Task ProcessPayment(Context ctx, decimal amount, string currency = "USD") 
{
    var paymentProcessor = new PaymentProcessorStub();
    var logger = new FakeLogger();
    
    ctx.SetAlways<IPaymentProcessor>(paymentProcessor, "mainProcessor")
       .SetOne<ILogger>(logger, "logger1")
       .SetOne<ILogger>(new FakeLogger(), "logger2");

    var service = new PaymentService();
    return service.ProcessPayment(amount, currency);
}
```

### CallLogger Wrapping
```csharp
[SpecRec]
public async Task TrackExternalCalls(Context ctx, string endpoint, int retryCount = 3) 
{
    var apiClient = new HttpApiClient();
    var trackedClient = ctx.Wrap(apiClient, '🔗');

    var service = new ExternalService(trackedClient);
    return service.FetchDataWithRetries(endpoint, retryCount);
}
```

### Parrot Creation
```csharp
[SpecRec]
public async Task ValidateInput(Context ctx, string input, bool strictMode = false) 
{
    var validator = ctx.CreateParrot<IValidator>('✅');
    
    var service = new ValidationService(validator);
    return service.ValidateUserInput(input, strictMode);
}
```

### Registering Objects
```csharp
[SpecRec]
public async Task ValidateInput(Context ctx, string input, bool strictMode = false) 
{
    var validator = ctx.CreateParrot<IValidator>('✅');
    ctx.Register(validator, 'PaymentValidator')
    
    var service = new ValidationService(validator);
    return service.ValidateUserInput(input, strictMode);
}
```

## Runtime Execution Flow

**User writes:**
```csharp
[SpecRec]
public async Task BookFlight(Context ctx, int passengerCount, string airlineCode = "UA") 
{
    ctx.Substitute<IBookingRepository>('💾');
    var coordinator = new BookingCoordinator();
    return coordinator.BookFlight(passengerCount, airlineCode);
}
```

**SpecRecDiscoverer execution flow:**
```csharp
// Discoverer creates Context and invokes user method inside wrapper
var ctx = new Context 
{ 
    CallLog = callLog, 
    TestCaseName = callLog.TestCaseName,
    Factory = ObjectFactory.Instance()
};

try 
{
    // Invoke user method via reflection with Context + parameters
    var result = await InvokeUserMethod("BookFlight", ctx, passengerCount, airlineCode);
    if (result != null)
    {
        callLog.AppendLine($"🔹 Returns: {ValueParser.FormatValue(result)}");
    }
}
catch (Exception ex) 
{
    callLog.AppendLine($"❌ Exception: {ex.GetType().Name}: {ex.Message}");
    throw;
}
finally 
{
    await callLog.Verify();
    ctx.Factory.ClearAll();
}
```

## Benefits

- **Developer Experience**: Write tests with minimal boilerplate
- **Test Clarity**: First parameter shows meaningful test case name
- **Type Safety**: Compile-time checking of input parameters with visible defaults
- **Resource Safety**: Automatic cleanup prevents test interference
- **Consistency**: Single API for all SpecRec operations
- **Migration Path**: Can coexist with existing test patterns

## Technical Considerations

- **Reflection Performance**: Method invocation via reflection has minimal overhead
- **Debugging**: User code runs directly - no generated code to complicate debugging
- **Error Handling**: Exception stack traces point to actual user code
- **IDE Support**: Standard xUnit integration - no special tooling required
- **Type Safety**: Compile-time parameter validation same as current approach