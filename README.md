# SpecRec for .NET

**Automated Legacy Testing Tools for .NET**

![Spec Rec Logo](./SpecRecLogo.png)

## Overview

SpecRec makes legacy code testable through automated instrumentation and transparent record-replay capabilities. By replacing direct object instantiation with controllable factories and wrapping dependencies with recording proxies, SpecRec eliminates the manual effort required to characterize and test existing systems.

**⚠️ This library is incomplete and under active development. Currently the ObjectFactory and CallLogger components are implemented.**

## Core Principles

- **Prioritize ease of use** - Hide complexity behind well-designed interfaces
- **Focus on public interactions** - Test behavior, not implementation details  
- **Minimal code changes** - Validate changes at a glance

## Current Components

### ObjectFactory

Replaces `new` keyword with controllable dependency injection for testing.

#### Usage example

Suppose you have an inconvenient `Repository` dependency that implements `IRepository`.

```csharp
class MyService 
{
    public void ComplexOperation() 
    {
        // Long and gnarly code
        var repository = new Repository("rcon://user:pwd@example.com/");
        // More code using the repository
    }
}
```

In many cases it is easy to break the dependency, but it can prove challenging if the call is several layers in.
In such situations you can use ObjectFactory to break the dependency with minimal change:

```csharp
using static SpecRec.GlobalObjectFactory;

class MyService 
{
    public void ComplexOperation() 
    {
        // Long and gnarly code
        var repository = Create<IRepository, Repository>("rcon://user:pwd@example.com/");
        var item = repository.FetchById(id);
        // More code using the repository
    }
}
```

Now you can easily inject a test double:

```csharp
public class MyServiceTests
{
    private readonly ObjectFactory factory = new();
    
    [Fact]
    public void TestWithTestDouble()
    {
        // Arrange
        var fakeRepo = new FakeRepository();
        factory.SetOne<IRepository>(fakeRepo);
        
        // Act
        MyService.ComplexOperation();
        
        // Assert
        // ...
    }
}
```

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


### CallLogger

Records method calls and constructor invocations to generate human-readable specifications for testing and documentation.

#### Usage example

When you have an untested legacy system it can become tedious to manually create tests. Part of that complexity
comes from setting up mocks/spies manually. 

The CallLogger solves this by creating a SpecBook that contains calls to specific outside collaborators that can
then be approved using an approval testing framework. 

First wrap the dependencies to automatically log all interactions:

```csharp
[Fact]
public async Task MyServiceTest()
{
    var logger = new CallLogger();
    
    // Create a dummy, fake or stub
    var fakeMessenger = new FakeMessenger();

    // Wrap it with the logger
    var loggedMessenger = logger.Wrap<IMessenger>(fakeMessenger, "📩");
    
    // Set up factory to return logged objects
    factory.SetOne<IMessenger>(loggedMessenger);
    
    // Execute the operation
    (new MyService()).Process();
    
    // Get human-readable specification
    var expectedLog = logger.SpecBook.ToString();
    
    // Verify the result
    await Verify(logger.SpecBook.ToString());
}
```

The resulting SpecBook will look something like this:

```
📩 SendMessage:
  🔸 recipient: user@example.com
  🔸 subject: Welcome
  🔸 body: Hello and welcome!
  🔹 Returns: true
```

#### Specbook Format

Method call format:
```
📩 MethodName:
  🔸 parameter_name: parameter_value
  🔸 out_parameter_name: out_parameter_value_before_the_call
  ♦️ out_parameter_name: out_parameter_value_after_the_call
  🔹 Returns: return_value
```

Constructor call format:
```
📩 IInterfaceName constructor called with:
  🔸 parameter_name: parameter_value
  🔸 parameter_name2: parameter_value2
```

#### Shared SpecBook

Sometimes you may want to add your own logs to the SpecBook. Just create a string builder and pass it in:

```csharp
var sharedSpecBook = new StringBuilder();
var logger = new CallLogger(sharedSpecBook);

sharedSpecBook.AppendLine("🧪 Test: User Authentication Flow");

var wrappedAuth = logger.Wrap<IAuthService>(authService, "🔐");
var wrappedUser = logger.Wrap<IUserService>(userService, "👤");

// Both services log to the same specification
wrappedAuth.Login("user", "pass");
wrappedUser.GetProfile(userId);

sharedSpecBook.AppendLine("✅ Authentication completed");
```


#### Constructor Logging

When used with the ObjectFactory, Objects implementing `IConstructorCalledWith` will have their constructor calls
logged as well. 

In some cases if a matching constructor is not found then the default `arg0`, `arg1` etc. names are used. 
If you want you can customize constructor parameter names for such constructor calls:

```csharp
public class DatabaseService : IDatabaseService, IConstructorCalledWith
{
    public DatabaseService(string connectionString, int timeout) { }
    
    public void ConstructorCalledWith(ConstructorParameterInfo[] parameters)
    {
        CallLogFormatterContext.SetConstructorArgumentNames("dbConnection", "timeoutSeconds");
    }
}
```


#### Controlling Log Output

Use `CallLogFormatterContext` within methods to control what gets logged:

```csharp
public void ProcessSecretData(string publicData, string secretKey)
{
    CallLogFormatterContext.IgnoreArgument(1); // Hide secretKey
    CallLogFormatterContext.AddNote("Processing with security protocols");
    // Method logic here
}

public string GetAuthToken()
{
    CallLogFormatterContext.IgnoreReturnValue(); // Hide sensitive return value
    return "secret-token";
}

public void InternalMethod()
{
    CallLogFormatterContext.IgnoreCall(); // Skip logging this call entirely
}
```

#### Manual Logging

Although not advised, you have the option to log calls manually. 

```csharp
var logger = new CallLogger();

// Build detailed call logs manually
logger.withArgument("user123", "userId")
    .withArgument(true, "isActive")
    .withNote("Validates user permissions")
    .withReturn("authorized")
    .log("CheckUserAccess");

var spec = logger.SpecBook.ToString();
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