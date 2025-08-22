# SpecRec for .NET

**Automated Legacy Testing Tools for .NET**

![Spec Rec Logo](./SpecRecLogo.png)

## Overview

SpecRec makes legacy code testable through automated instrumentation and transparent record-replay capabilities. By replacing direct object instantiation with controllable factories and wrapping dependencies with recording proxies, SpecRec eliminates the manual effort required to characterize and test existing systems.

**⚠️ This library is incomplete and under active development. Currently the ObjectFactory, CallLogger, and Parrot components are implemented.**

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
  🔸 recipient: "user@example.com"
  🔸 subject: "Welcome"
  🔸 body: "Hello and welcome!"
  🔹 Returns: true
```

#### Specbook Format

Method call format:
```
📩 MethodName:
  🔸 parameter_name: "parameter_value"
  🔸 out_parameter_name: "out_parameter_value_before_the_call"
  ♦️ out_parameter_name: "out_parameter_value_after_the_call"
  🔹 Returns: "return_value"
```

Constructor call format:
```
📩 IInterfaceName constructor called with:
  🔸 parameter_name: "parameter_value"
  🔸 parameter_name2: "parameter_value2"
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


## Object ID Tracking

SpecRec now supports automatic object identification in tests, replacing verbose object serialization with clean ID references.

### Quick Example

```csharp
// Setup
var factory = new ObjectFactory();
var emailService = new EmailService();
factory.Register(emailService, "emailSvc");

// Logging (produces clean output)
var logger = new CallLogger(objectFactory: factory);
var wrappedUserService = logger.Wrap<IUserService>(userService, "🔧");
wrappedUserService.SendWelcomeEmail(emailService); // Logs as <id:emailSvc>

// Replay (Parrot resolves IDs back to objects)
var callLog = new CallLog(logger.SpecBook.ToString(), factory);
var parrot = Parrot.Create<IUserService>(callLog, "🦜", factory);
var result = parrot.SendWelcomeEmail(emailService); // Resolves emailSvc back to original object
```

### Migrating Existing Tests

No changes required for existing tests. Object tracking is opt-in:

```csharp
// Before (still works)
var logger = new CallLogger();

// After (with object tracking)
var factory = new ObjectFactory();
factory.Register(myService, "myService");
var logger = new CallLogger(objectFactory: factory);
```

### Best Practices

1. **Use descriptive IDs**: `"userDb"`, `"emailSvc"` instead of `"obj1"`, `"obj2"`
2. **Register before wrapping**: Register all objects before creating wrapped services
3. **Consistent ObjectFactory**: Use the same ObjectFactory instance for logging and replay
4. **Auto-generated IDs**: Let ObjectFactory generate descriptive IDs with `SetOne()` and `SetAlways()`

### How It Works

When you register objects with the ObjectFactory:

```csharp
var factory = new ObjectFactory();
var emailService = new EmailService();
var databaseService = new DatabaseService();

factory.Register(emailService, "emailSvc");
factory.Register(databaseService, "userDb");
```

CallLogger will format registered objects as clean ID references:

```
🔧 ProcessUser:
  🔸 emailService: <id:emailSvc>
  🔸 database: <id:userDb>
  🔸 userId: "user123"
  🔹 Returns: true
```

Instead of verbose object dumps:

```
🔧 ProcessUser:
  🔸 emailService: {Name: "EmailService", ConnectionString: "smtp://..."}
  🔸 database: {Name: "DatabaseService", Provider: "SqlServer", ...}
  🔸 userId: "user123"
  🔹 Returns: true
```

When replaying with Parrot, the IDs are resolved back to the original objects:

```csharp
var callLog = new CallLog(logger.SpecBook.ToString(), factory);
var parrot = Parrot.Create<IUserService>(callLog, "🦜", factory);

// Parrot automatically resolves <id:emailSvc> back to the original emailService object
var result = parrot.ProcessUser(emailService, databaseService, "user123");
```

### Error Handling

If you forget to register an object, CallLogger will output `<unknown>`:

```
🔧 ProcessUser:
  🔸 emailService: <unknown>
  🔹 Returns: true
```

When Parrot encounters `<unknown>` in a verified file, it throws a helpful error:

```
ParrotUnknownObjectException: Encountered <unknown> object in verified file. 
Register all objects with ObjectFactory before running tests.
```

### Registry Management

The ObjectFactory provides full registry management:

```csharp
// Check if object is registered
bool isRegistered = factory.IsRegistered(myObject);

// Get the ID for a registered object
string? id = factory.GetRegisteredId(myObject);

// Get the object for a registered ID
MyService? obj = factory.GetRegisteredObject<MyService>("myServiceId");

// Auto-generate descriptive IDs for SetOne/SetAlways
factory.SetOne(emailService); // Auto-generates "EmailService_1"
factory.SetAlways(databaseService); // Auto-generates "DatabaseService_2"
```


### Parrot Test Double

Provides intelligent test doubles that replay method calls and return values from verified specification files. Parrot combines the advanced logging capabilities of CallLogger with a Stub to further simplify creating gold master tests.

#### Usage example

When testing complex integrations, manually creating test doubles with all expected return values becomes tedious. Parrot automates this by reading verified specification files and replaying the exact return values.

First, create your test and run it to generate the specification:

```csharp
[Fact]
public async Task ComplexIntegration_ShouldHandleMultipleCalls()
{
    var callLog = CallLog.FromVerifiedFile(); // Loads from verified file
    var service = Parrot.Create<IExternalService>(callLog, "🦜");
    
    try 
    {
        // Make calls - first run will throw exceptions for missing return values
        var message = service.GetMessage(200);
        Assert.Equal("Success", message);
        
        service.SendMessage("test");
        
        
        var optionalValue = service.GetOptionalValue("missing");
        Assert.Null(optionalValue);
    }
    finally 
    {
        await Verify(callLog.ToString());
    }
}
```

On the first run, Parrot throws `ParrotMissingReturnValueException` and generates a `.received.txt` file:

```
🦜 GetMessage:
  🔸 code: 200
  🔹 Returns: <missing_value>
```

Replace `<missing_value>` with the expected return values and rename to `.verified.txt`.

```
🦜 GetMessage:
  🔸 code: 200
  🔹 Returns: "Success"
```

This time the test will continue running until the next missing return value:

```
🦜 GetMessage:
  🔸 code: 200
  🔹 Returns: "Success"

🦜 SendMessage:
  🔸 input: "test"

🦜 GetOptionalValue:
  🔸 key: "missing"
  🔹 Returns: <missing_value>
```

Let's specify this return value as well:

```
🦜 GetMessage:
  🔸 code: 200
  🔹 Returns: "Success"

🦜 SendMessage:
  🔸 input: "test"

🦜 GetOptionalValue:
  🔸 key: "missing"
  🔹 Returns: null
```

Now the test passes! Parrot replays the exact return values from the verified file.

#### Creating Parrot Test Doubles

```csharp
// Basic usage with default parrot emoji
var parrot = Parrot.Create<IMyService>(callLog);

// Custom emoji for better visual distinction
var parrot = Parrot.Create<IMyService>(callLog, "🎭");

// Load from specific verified file
var callLog = CallLog.FromFile("path/to/verified.txt");
var parrot = Parrot.Create<IMyService>(callLog, "🔧");
```

#### Automatic Verified File Discovery

Parrot automatically discovers verified files based on test method names:

```csharp
[Fact]
public async Task MyComplexTest()
{
    // Automatically looks for: MyComplexTest.verified.txt
    var callLog = CallLog.FromVerifiedFile();
    var service = Parrot.Create<IExternalService>(callLog);
    
    // Test implementation
}
```

#### Exception Handling

Parrot provides specific exceptions for different failure scenarios:

```csharp
try
{
    var result = service.ProcessData("input");
}
catch (ParrotMissingReturnValueException ex)
{
    // No return value specified in verified file
    // Update .verified.txt with expected return value
}
catch (ParrotCallMismatchException ex)
{
    // Method called with different arguments than expected
    // Check test logic or update verified file
}
catch (ParrotTypeConversionException ex)
{
    // Return value cannot be converted to expected type
    // Fix return value format in verified file
}
```

#### Workflow Integration

Use Parrot with approval testing frameworks for a complete testing workflow:

```csharp
[Fact]
public async Task EndToEndWorkflow()
{
    var callLog = CallLog.FromVerifiedFile();
    
    // Set up multiple services with different emojis
    var authService = Parrot.Create<IAuthService>(callLog, "🔐");
    var userService = Parrot.Create<IUserService>(callLog, "👤");
    var emailService = Parrot.Create<IEmailService>(callLog, "📧");
    
    factory.SetOne<IAuthService>(authService);
    factory.SetOne<IUserService>(userService);
    factory.SetOne<IEmailService>(emailService);
    
    // Execute complex business logic
    await businessLogic.ProcessNewUser("john@example.com");
    
    // Verify complete interaction log
    await Verify(callLog.ToString());
}
```


### SpecRecLogsAttribute

Enables data-driven testing by automatically discovering verified files and generating individual test cases for each scenario. Works with xUnit Theory to create multiple test executions from a single test method.

#### Usage example

When you are creating tests for an untested sub system you may find that multiple tests use the same set of Parrot Test Doubles and preconditions. In these scenarios you can use the `[SpecRecLogs]` annotation to create a Theory with multiple test cases. 

```csharp
[Theory]
[SpecRecLogs]
public async Task TestMultipleScenarios(string testCaseName)
{
    var callLog = CallLog.ForTestCase(testCaseName);
    var reader = Parrot.Create<IInputReader>(callLog);
    var calculator = Parrot.Create<ICalculatorService>(callLog);

    var result = ProcessInput(reader, calculator);

    callLog.AppendLine($"Result was: {result}");
    await callLog.Verify();
}

private int ProcessInput(IInputReader reader, ICalculatorService calculator)
{
    var values = reader.NextValues();
    switch (reader.NextOperation())
    {
        case "add":
            return calculator.Add(values[0], values[1]);
        case "multiply":
            return calculator.Multiply(values[0], values[1]);
        default:
            throw new Exception("Unknown operation");
    }
}
```

#### File Naming Convention

SpecRecLogsAttribute discovers verified files using the pattern:
```
{ClassName}.{MethodName}.{TestCaseName}.verified.txt
```

For example:
- `MultiFixture.TestMultipleScenarios.AddTwoNumbers.verified.txt`
- `MultiFixture.TestMultipleScenarios.MultiplyNumbers.verified.txt`
- `MultiFixture.TestMultipleScenarios.AddZeroes.verified.txt`

#### Test Case Generation

Each verified file becomes a separate test case:

```
✓ TestMultipleScenarios(testCaseName: "AddTwoNumbers")
✓ TestMultipleScenarios(testCaseName: "MultiplyNumbers")  
✓ TestMultipleScenarios(testCaseName: "AddZeroes")
```

#### Creating Test Cases

1. Write your test method with `[Theory]` and `[SpecRecLogs]`
2. Run the test - it will fail with "FirstTestCase" if no files exist
3. Create verified files for each scenario you want to test
4. Each file will become a separate test case automatically

#### Verified File Content

Each verified file contains the expected interactions for that test case:

**AddTwoNumbers.verified.txt:**
```
🦜 NextValues:
  🔹 Returns: [5, 3]

🦜 NextOperation:
  🔹 Returns: "add"

🦜 Add:
  🔸 a: 5
  🔸 b: 3
  🔹 Returns: 8

Result was: 8
```

**MultiplyNumbers.verified.txt:**
```
🦜 NextValues:
  🔹 Returns: [7, 4]

🦜 NextOperation:
  🔹 Returns: "multiply"

🦜 Multiply:
  🔸 a: 7
  🔸 b: 4
  🔹 Returns: 28

Result was: 28
```

#### Integration with CallLog.ForTestCase

Use `CallLog.ForTestCase(testCaseName)` to automatically load the correct verified file for each test case and set up the test context properly.

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

- **Automated test discovery**: Generates call logs automatically to create 100% branch coverage of SUT
- **Instrumentation Interface**: Refactoring tools to break inconvenient dependencies 

## Requirements

- .NET 9.0+
- C# 13+

## License

PolyForm Noncommercial License 1.0.0