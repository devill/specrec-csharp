# SpecRec for .NET

**Automated Legacy Testing Tools for .NET**

## Overview

SpecRec makes legacy code testable through automated instrumentation and transparent record-replay capabilities. By replacing direct object instantiation with controllable factories and wrapping dependencies with recording proxies, SpecRec eliminates the manual effort required to characterize and test existing systems.

**⚠️ This library is incomplete and under active development. Currently only the ObjectFactory component is implemented.**

## Core Principles

- **Prioritize ease of use** - Hide complexity behind well-designed interfaces
- **Focus on public interactions** - Test behavior, not implementation details  
- **Minimal code changes** - Validate changes at a glance

## Current Components

### ObjectFactory

Replaces `new` keyword with controllable dependency injection for testing.

#### Basic Usage

```csharp
// Create objects normally
var factory = new ObjectFactory();
var obj = factory.Create<MyClass>();

// With constructor arguments
var obj = factory.Create<MyClass>("arg1", 42);

// Interface/implementation pattern
var obj = factory.Create<IMyInterface, MyImplementation>();
```

#### Test Object Injection

```csharp
// Queue specific objects for testing
var mockObj = new MyMockClass();
factory.SetOne<MyClass>(mockObj);

var result = factory.Create<MyClass>();
// result == mockObj

// Always return the same object
factory.SetAlways<MyClass>(mockObj);
```

#### Global Factory (Static Access)

```csharp
using static SpecRec.GlobalObjectFactory;

// Use anywhere without creating factory instances
var obj = Create<MyClass>("arg1", 42);
var obj = Create<IMyInterface, MyImplementation>();
```

#### Constructor Parameter Logging

Objects implementing `IConstructorCalledWith` receive detailed parameter information:

```csharp
public class MyMock : IMyInterface, IConstructorCalledWith
{
    private ConstructorParameterInfo[] lastParams;
    
    public void ConstructorCalledWith(ConstructorParameterInfo[] parameters)
    {
        this.lastParams = parameters;
        // parameters[0].Name == "username"
        // parameters[0].Type == typeof(string)
        // parameters[0].Value == "john"
    }
}
```

#### Management Operations

```csharp
// Clear specific type
factory.Clear<MyClass>();

// Clear all registered objects
factory.ClearAll();
```

## NuGet Package

Add to your project:

```xml
<PackageReference Include="SpecRec" Version="0.0.1" />
```

Or via Package Manager Console:

```powershell
Install-Package SpecRec
```

## Test Integration

Works seamlessly with popular .NET testing frameworks:

### xUnit Example

```csharp
public class MyServiceTests
{
    private readonly ObjectFactory factory = new();
    
    [Fact]
    public void TestWithMockedDependency()
    {
        // Arrange
        var mockRepo = new MockRepository();
        factory.SetOne<IRepository>(mockRepo);
        
        // Act
        var service = factory.Create<MyService>();
        var result = service.ProcessData("test");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", mockRepo.LastQuery);
    }
}
```

## Planned Components

- **Surveyor**: Transparent spy/mock that logs all interactions
- **SpecReplay**: Replay recorded specifications  
- **SpecBook**: Human-readable test recording format
- **Instrumentation Interface**: Language-specific automation tools

## Requirements

- .NET 9.0+
- C# 13+

## License

PolyForm Noncommercial License 1.0.0