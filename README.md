# SpecRec for .NET

**Turn untestable legacy code into comprehensive test suites in minutes**

![Spec Rec Logo](./SpecRecLogo.png)

## Why SpecRec?

Testing legacy code is painful. You spend hours setting up mocks, understanding complex dependencies, and writing brittle tests that break when implementation details change. 

SpecRec solves this by:
- **Recording** actual method calls and return values from your legacy code
- **Generating** human-readable specifications automatically
- **Replaying** those interactions as fast, reliable test doubles

No more guessing what your code does - SpecRec shows you exactly what happens.

## Installation

Add to your test project:

```xml
<PackageReference Include="SpecRec" Version="0.0.4" />
<PackageReference Include="Verify.Xunit" Version="26.6.0" />
```

Or via Package Manager Console:

```powershell
Install-Package SpecRec
Install-Package Verify.Xunit
```

## The Complete Picture

Here's what a finished SpecRec test looks like - clean, fast, and comprehensive:

```csharp
[Fact]
public async Task UserRegistration_ShouldWork()
{
    using static GlobalObjectFactory;
    
    // Initialize a CallLog
    var callLog = CallLog.FromVerifiedFile();
 
    // Set up test doubles
    var emailParrot = Parrot.Create<IEmailService>(callLog, "📧");
    var dbParrot = Parrot.Create<IDatabaseService>(callLog, "🗃️");
    
    // Inject the test doubles
    SetOne<IEmailService>(emailParrot, "email");
    SetOne<IDatabaseService>(dbParrot, "userDb");
    
    // Act: call your legacy code
    (new UserService()).RegisterNewUser("john@example.com", "John Doe");
    
    // Verify the call log
    await callLog.Verify();
}
```

**That's it!** When the test runs it creates a log of all interactions you can approve by coping the `.received.txt` file to the corresponding `.verified.txt`.

**UserRegistration_ShouldWork.verified.txt:**
```
📧 SendWelcomeEmail:
  🔸 recipient: "john@example.com"
  🔸 subject: "Welcome John Doe"
  🔹 Returns: True

🗃️ CreateUser:
  🔸 email: "john@example.com"
  🔸 name: "John Doe" 
  🔹 Returns: 42
```

## How to Get There: The 6-Step SpecRec Workflow

1. **Identify dependencies** - Find objects outside your system under test that need test doubles
2. **Break the dependencies** - Change `new EmailService()` to `factory.Create<EmailService>()` so you can control what gets created
3. **Create the test doubles** - SpecRec uses a Spy called Parrot to log all interactions
4. **Run the test** - It will fail with "missing return value" exceptions
5. **Fix return values** - Copy `.received.txt` to `.verified.txt` and fill in expected return values
6. **Repeat until green** - Each run reveals the next missing return value until your test passes

**Result:** A fast, reliable characterization test that documents exactly what your legacy code does.

**⚠️ This library is under active development. Core components (ObjectFactory, CallLogger, Parrot) are stable.**

## How It Works

SpecRec has three main components that work together:

1. **ObjectFactory** - Makes dependencies controllable for testing
2. **CallLogger** - Records method calls and return values
3. **Parrot** - Replays recorded interactions as test doubles  

## ObjectFactory: Breaking Dependencies

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
public class MyServiceTests : IDisposable
{
    public MyServiceTests()
    {
        ObjectFactory.Instance().ClearAll();
    }
    
    public void Dispose()
    {
        ObjectFactory.Instance().ClearAll();
    }
    
    [Fact]
    public void TestWithTestDouble()
    {
        using static GlobalObjectFactory;
        
        // Arrange
        var fakeRepo = new FakeRepository();
        ObjectFactory.Instance().SetOne<IRepository>(fakeRepo);
        
        // Act
        MyService.ComplexOperation();
        
        // Assert
        // ...
    }
}
```

**Recommended approach:** Use the global `ObjectFactory.Instance()` with proper test setup/teardown to avoid test isolation issues. The constructor and `Dispose` methods ensure each test starts with a clean factory state.

#### Basic Usage (Recommended: Global Factory)

```csharp
using static SpecRec.GlobalObjectFactory;

// Create objects using the global factory (recommended)
var obj = Create<MyClass>();

// With constructor arguments
var obj = Create<MyClass>("arg1", 42);

// Interface/implementation pattern
var obj = Create<IMyInterface, MyImplementation>();
```

#### Test Object Injection

```csharp
using static SpecRec.GlobalObjectFactory;

// Queue specific objects for testing
var mockObj = new MyMockClass();
ObjectFactory.Instance().SetOne<MyClass>(mockObj);

var result = Create<MyClass>();
// result == mockObj

// Always return the same object
ObjectFactory.Instance().SetAlways<MyClass>(mockObj);
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
ObjectFactory.Instance().Clear<MyClass>();

// Clear all registered objects  
ObjectFactory.Instance().ClearAll();
```

#### Local Factory (Alternative Pattern)

```csharp
// Alternative: Use local factory instances for specific scenarios
var factory = new ObjectFactory();
var obj = factory.Create<MyClass>();
factory.SetOne<MyClass>(mockObj);
factory.ClearAll();
```

**Note:** While local factories work fine, the global factory is recommended for most scenarios as it simplifies test setup and integrates better with SpecRec's other components.


## CallLogger: Recording Method Calls

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
    
    // Set up global factory to return logged objects
    ObjectFactory.Instance().SetOne<IMessenger>(loggedMessenger);
    
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
// Setup (recommended: use global factory)
var factory = ObjectFactory.Instance();
var emailService = new EmailService();
factory.Register(emailService, "emailSvc");

// Logging (produces clean output)
var logger = new CallLogger();
var wrappedUserService = logger.Wrap<IUserService>(userService, "🔧");
wrappedUserService.SendWelcomeEmail(emailService); // Logs as <id:emailSvc>

// Result: Clean, readable specification
Console.WriteLine(logger.SpecBook.ToString());
```

### Migrating Existing Tests

No changes required for existing tests. Object tracking is opt-in:

```csharp
// Before (still works)
var logger = new CallLogger();

// After (with object tracking)
var factory = ObjectFactory.Instance();
factory.Register(myService, "myService");
var logger = new CallLogger();
```

### Best Practices

1. **Use descriptive IDs**: `"userDb"`, `"emailSvc"` instead of `"obj1"`, `"obj2"`
2. **Register before wrapping**: Register all objects before creating wrapped services
3. **Consistent ObjectFactory**: Use the same ObjectFactory instance for logging and replay
4. **Auto-generated IDs**: Let ObjectFactory generate descriptive IDs with `SetOne()` and `SetAlways()`

### How It Works

When you register objects with the global ObjectFactory:

```csharp
var factory = ObjectFactory.Instance();
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
var factory = ObjectFactory.Instance();

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

## Type-Safe Value Parsing and Formatting

SpecRec uses a type-aware parser that ensures robust handling of values in specification files. The `ValueParser` enforces strict formatting rules to eliminate ambiguity and prevent type mismatches during test replay.

### Strict Formatting Rules

**Strings**: Must be quoted with double quotes
```
✅ "hello world"
❌ hello world (unquoted strings not supported)
```

**Booleans**: Case-sensitive `True` and `False`
```
✅ True, False
❌ true, false, TRUE, FALSE
```

**Numbers**: Use invariant culture formatting
```
✅ 42 (integer)
✅ 3.14 (decimal)
✅ -123 (negative)
```

**Arrays**: Square brackets with comma-separated values
```
✅ [1,2,3]
✅ ["item1","item2","item3"]
❌ [1, 2, 3] (spaces not supported)
```

**Object References**: Use Object ID format
```
✅ <id:emailService> (registered object)
✅ <unknown:EmailService> (unregistered object with type info)
❌ <unknown> (deprecated, lacks type information)
```

**Null Values**: Simple lowercase `null`
```
✅ null
```

### Type-Safe Parsing

The parser requires the target type to be known, eliminating guesswork:

- **Parse-only-when-converting**: Raw string values are stored during file parsing, then converted to the correct type only when needed
- **Immediate error validation**: Invalid formats like `<unknown>` or empty object IDs fail immediately during file loading
- **Type compatibility checking**: Object ID resolution validates that resolved objects can be assigned to the expected type

### Enhanced Error Messages

When objects aren't registered in the ObjectFactory, SpecRec now provides helpful type information:

**Before**: `<unknown>`  
**After**: `<unknown:EmailService>`

This improvement makes it easier to identify which objects need to be registered for tests to work properly.

### Backward Compatibility

The parsing system maintains compatibility with existing verified files while enforcing stricter rules for new content. Legacy `<unknown>` markers are still supported but will be gradually replaced with the enhanced `<unknown:TypeName>` format for better debugging.


## Parrot: Intelligent Test Doubles

Provides intelligent test doubles that replay method calls and return values from verified specification files. Parrot eliminates the tedious work of manually setting up stubs by automatically replaying the exact interactions from your verified specification files.

When used with Object ID Tracking, Parrot automatically resolves `<id:objectName>` references back to the original registered objects, enabling seamless replay of complex object interactions.

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
    
    var factory = ObjectFactory.Instance();
    factory.SetOne<IAuthService>(authService);
    factory.SetOne<IUserService>(userService);
    factory.SetOne<IEmailService>(emailService);
    
    // Execute complex business logic
    await businessLogic.ProcessNewUser("john@example.com");
    
    // Verify complete interaction log
    await Verify(callLog.ToString());
}
```


## SpecRecLogsAttribute: Data-Driven Testing

Enables data-driven testing by automatically discovering verified files and generating individual test cases for each scenario. Supports test input parameters extracted from preamble sections.

#### Usage example

When you are creating tests for an untested sub system you may find that multiple tests use the same set of Parrot Test Doubles and preconditions. In these scenarios you can use the `[SpecRecLogs]` annotation to create a Theory with multiple test cases. 

```csharp
[Theory]
[SpecRecLogs]
public async Task TestMultipleScenarios(CallLog callLog)
{
    var reader = Parrot.Create<IInputReader>(callLog);
    var calculator = Parrot.Create<ICalculatorService>(callLog);

    var result = ProcessInput(reader, calculator);

    callLog.AppendLine($"Result was: {result}");
    await callLog.Verify();
}

// With test input parameters
[Theory]
[SpecRecLogs]
public async Task TestWithUserData(CallLog callLog, string userName, bool isAdmin, int age)
{
    var service = Parrot.Create<IUserService>(callLog);
    
    var user = service.CreateUser(userName, isAdmin, age);
    
    callLog.AppendLine($"Created user: {userName} (Admin: {isAdmin}, Age: {age})");
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

**With test input parameters (TestWithUserData.verified.txt):**
```
📋 <Test Inputs>
  🔸 userName: "john.doe"
  🔸 isAdmin: True
  🔸 age: 25

🦜 CreateUser:
  🔸 name: "john.doe"
  🔸 isAdmin: True
  🔸 age: 25
  🔹 Returns: <id:user1>

Created user: john.doe (Admin: True, Age: 25)
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

#### Test Input Parameters

Parameters are extracted from preamble sections and passed to test methods. Supports built-in types, arrays, and dictionaries. Error messages provide copy-paste preamble sections when parameters are missing.

#### Default Parameter Values

SpecRecLogs supports default parameter values, allowing you to omit common parameters from verified files and only specify values that differ from defaults:

```csharp
[Theory]
[SpecRecLogs]
public async Task TestUser(CallLog callLog, string userName = "John Doe", bool isAdmin = false, int age = 34)
{
    var service = Parrot.Create<IUserService>(callLog);
    
    var user = service.CreateUser(userName, isAdmin, age);
    
    callLog.AppendLine($"Created user: {userName} (Admin: {isAdmin}, Age: {age})");
    await callLog.Verify();
}
```

**Verified files can now omit parameters with defaults:**

**AllDefaults.verified.txt** (uses all default values):
```
🦜 CreateUser:
  🔸 name: "John Doe"
  🔸 isAdmin: False  
  🔸 age: 34
  🔹 Returns: <id:user1>

Created user: John Doe (Admin: False, Age: 34)
```

**PartialOverride.verified.txt** (overrides only isAdmin):
```
📋 <Test Inputs>
  🔸 isAdmin: True

🦜 CreateUser:
  🔸 name: "John Doe"
  🔸 isAdmin: True
  🔸 age: 34
  🔹 Returns: <id:user2>

Created user: John Doe (Admin: True, Age: 34)
```

This eliminates repetition when many test cases share the same base values, while still allowing full customization when needed.

## Planned Components

- **Automated test discovery**: Generates call logs automatically to create 100% branch coverage of SUT
- **Instrumentation Interface**: Refactoring tools to break inconvenient dependencies 

## Requirements

- .NET 9.0+
- C# 13+
- xUnit (for examples) - any test framework works
- Verify framework (for approval testing)

## License

PolyForm Noncommercial License 1.0.0