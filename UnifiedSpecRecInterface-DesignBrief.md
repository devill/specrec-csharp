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
    // Substitute - sets up ObjectFactory to create new parrots automatically for T
    // Every time ObjectFactory.Create<T>() is called, creates a new parrot and registers it with a unique ID
    public Context Substitute<T>(string icon = "🔧") where T : class;
    
    // CallLogger operations - work with existing objects (simple delegators)
    public T Wrap<T>(T obj, string icon = "🔧") where T : class;   // Wraps but doesn't register
    public T Parrot<T>(string icon = "🦜") where T : class;        // Creates parrot but doesn't register

    // ObjectFactory operations - register existing objects
    public Context SetAlways<T>(T obj, string? id = null) where T : class;
    public Context SetOne<T>(T obj, string? id = null) where T : class;
    public Context Register<T>(T obj, string id) where T : class;
    
    // Display name for test parameters
    public override string ToString() => TestCaseName ?? "DefaultCase";

    // Internal properties (framework use)
    internal string? TestCaseName { get; set; }
    internal CallLog CallLog { get; set; }
    internal ObjectFactory Factory { get; set; }
    internal CallLogger CallLogger { get; set; } // Instantiated with CallLog and Factory
    internal Parrot Parrot { get; set; }  // Instantiated with CallLog and Factory
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
    ctx.Substitute<IBookingRepository>("💾")
       .Substitute<IFlightService>("✈️");

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
    var trackedClient = ctx.Wrap(apiClient, "🔗");

    var service = new ExternalService(trackedClient);
    return service.FetchDataWithRetries(endpoint, retryCount);
}
```

### Parrot Creation
```csharp
[SpecRec]
public async Task ValidateInput(Context ctx, string input, bool strictMode = false) 
{
    var validator = ctx.Parrot<IValidator>("✅");
    
    var service = new ValidationService(validator);
    return service.ValidateUserInput(input, strictMode);
}
```

### Registering Objects
```csharp
[SpecRec]
public async Task ValidateInput(Context ctx, string input, bool strictMode = false) 
{
    var validator = ctx.Parrot<IValidator>("✅");
    ctx.Register(validator, "PaymentValidator");
    
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
    ctx.Substitute<IBookingRepository>("💾");
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
catch (ParrotMissingReturnValueException)
{
    throw;
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

## ObjectFactory Extensions Required

To support the `Substitute<T>()` functionality, the ObjectFactory needs to be extended with new capabilities:

```csharp
public class ObjectFactory 
{
    // Existing methods...
    
    // New method: Set up automatic parrot substitution for type T
    public void SetAutoParrot<T>(CallLog callLog, string icon) where T : class
    {
        // When Create<T>() is called, automatically:
        // 1. Create a new Parrot<T> with the specified icon
        // 2. Generate a unique ID (e.g., "T_1", "T_2", etc.)
        // 3. Register the parrot with the generated ID
        // 4. Return the parrot instance
    }
    
    // Enhanced Create method that checks for auto-substitutes
    public T Create<T>(params object[] args) where T : class
    {
        // Check if T has auto-substitute configured
        if (HasAutoSubstitute<T>())
        {
            return CreateAutoSubstitute<T>();
        }
        
        // Fall back to normal creation logic
        return base.Create<T>(args);
    }
}
```

This enables the seamless `ctx.Substitute<IService>("🔧")` followed by normal `ObjectFactory.Create<IService>()` calls in the system under test.

## Technical Considerations

- **Reflection Performance**: Method invocation via reflection has minimal overhead
- **Debugging**: User code runs directly - no generated code to complicate debugging
- **Error Handling**: Exception stack traces point to actual user code
- **IDE Support**: Standard xUnit integration - no special tooling required
- **Type Safety**: Compile-time parameter validation same as current approach