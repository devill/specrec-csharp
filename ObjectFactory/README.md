# ObjectFactory

Lightweight dependency injection for legacy code. Replace `new` with testable factories without major refactoring.

## Why ObjectFactory?

Legacy code often has hard-coded dependencies that make testing impossible:

```csharp
public class OrderProcessor
{
    public void Process(Order order)
    {
        var email = new EmailService(); // Can't test this!
        email.Send(order.Customer.Email, "Order confirmed");
    }
}
```

ObjectFactory lets you make code testable with minimal changes:

```csharp
using static ObjectFactory.GlobalObjectFactory;

public class OrderProcessor
{
    public void Process(Order order)
    {
        var email = Create<IEmailService, EmailService>(); // Now testable!
        email.Send(order.Customer.Email, "Order confirmed");
    }
}
```

## Installation

```xml
<PackageReference Include="ObjectFactory" Version="1.0.0" />
```

## Usage

### Basic Creation

```csharp
using static ObjectFactory.GlobalObjectFactory;

// Create with interface mapping
var service = Create<IEmailService, EmailService>();

// Create with constructor parameters
var repo = Create<IRepository, SqlRepository>(connectionString);

// Create concrete class
var processor = Create<OrderProcessor>(config, logger);
```

### Testing with Test Doubles

```csharp
[Fact]
public void TestOrderProcessing()
{
    // Arrange
    var factory = ObjectFactory.ObjectFactory.Instance();
    factory.ClearAll(); // Clean state

    var mockEmail = new MockEmailService();
    factory.SetOne<IEmailService>(mockEmail);

    // Act
    var processor = new OrderProcessor();
    processor.Process(testOrder);

    // Assert
    Assert.True(mockEmail.WasCalled);

    // Cleanup
    factory.ClearAll();
}
```

### SetOne vs SetAlways

```csharp
// SetOne: Queued, used once then removed
factory.SetOne<IService>(mock1);
factory.SetOne<IService>(mock2);
var s1 = Create<IService>(); // Returns mock1
var s2 = Create<IService>(); // Returns mock2
var s3 = Create<IService>(); // Creates new instance

// SetAlways: Persistent, returned every time
factory.SetAlways<IService>(mock);
var s1 = Create<IService>(); // Returns mock
var s2 = Create<IService>(); // Returns mock (same instance)
```

### Custom Factories

For advanced scenarios, inject custom factory functions:

```csharp
factory.SetFactory<IService>(args => {
    // Custom logic to create instances
    var config = (Config)args[0];
    return config.UseProduction
        ? new ProductionService()
        : new TestService();
});
```

## Integration with Testing Frameworks

ObjectFactory works with any .NET testing framework (xUnit, NUnit, MSTest). It also integrates seamlessly with SpecRec for automated testing workflows.

## License

PolyForm Noncommercial License 1.0.0
