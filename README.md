# SpecRec for .NET

**Turn untestable legacy code into comprehensive test suites in minutes**

![Spec Rec Logo](./SpecRecLogo.png)

## Introduction: From Legacy Code to Tests in 3 Steps

SpecRec helps you test legacy code by recording real method calls and replaying them as test doubles. Here's the complete workflow:

### Step 1: Break Dependencies with Create<>

Replace direct instantiation (`new`) with `Create<>` to make dependencies controllable:

```csharp
// Before: Hard dependency
var emailService = new EmailService(connectionString);

// After: Testable dependency
using static SpecRec.GlobalObjectFactory;
var emailService = Create<IEmailService, EmailService>(connectionString);
```

### Step 2: Write a Test with ctx.Verify

Create a test that uses SpecRec's Context API to automatically record and verify interactions:

```csharp
[Theory]
[SpecRecLogs]
public async Task UserRegistration(Context ctx, string email = "john@example.com", string name = "John Doe")
{
    await ctx.Verify(async () =>
    {
        // Set up automatic test doubles
        ctx.Substitute<IEmailService>("📧")
           .Substitute<IDatabaseService>("🗃️");
        
        // Run your legacy code
        var userService = new UserService();
        return userService.RegisterNewUser(email, name);
    });
}
```

### Step 3: Run Test and Fill Return Values

First run generates a `.received.txt` file with `<missing_value>` placeholders:

```
📧 SendWelcomeEmail:
  🔸 recipient: "john@example.com"
  🔸 subject: "Welcome!"
  🔹 Returns: <missing_value>
```

Replace `<missing_value>` with actual values and save as `.verified.txt`:

The next run will stop at the next missing return value:

```
📧 SendWelcomeEmail:
  🔸 recipient: "john@example.com"
  🔸 subject: "Welcome!"
  🔹 Returns: True

🗃️ CreateUser:
  🔸 email: "john@example.com"
  🔹 Returns: <missing_value>
```

Repeat until the test passes! SpecRec's Parrot replays these exact return values whenever your code calls these methods.

### Understanding Parrot

Parrot is SpecRec's intelligent test double that:
- Reads your verified specification files
- Matches incoming method calls by name and parameters
- Returns the exact values you specified

This means you never have to manually set up mocks - just provide the return values once and Parrot handles the rest.

## Installation

Add to your test project:

```xml
<PackageReference Include="SpecRec" Version="1.0.1" />
<PackageReference Include="Verify.Xunit" Version="26.6.0" />
```

Or via Package Manager Console:

```powershell
Install-Package SpecRec
Install-Package Verify.Xunit
```

## Core Components

### ObjectFactory: Making Dependencies Testable

**Use Case:** Your legacy code creates dependencies with `new`, making it impossible to inject test doubles.

**Solution:** Replace `new` with `Create<>` to enable dependency injection without major refactoring.

#### With Context API (Recommended for SpecRec Tests)

```csharp
[Theory]
[SpecRecLogs]
public async Task MyTest(Context ctx)
{
    await ctx.Verify(async () =>
    {
        // Automatically injects test doubles for Create<IRepository>
        ctx.Substitute<IRepository>("🗄️");
        
        // Your code can now use:
        var repo = Create<IRepository>();  // Gets the test double
    });
}
```

#### In Regular Tests

```csharp
[Fact]
public void RegularTest()
{
    // Setup
    ObjectFactory.Instance().ClearAll();
    
    var mockRepo = new MockRepository();
    ObjectFactory.Instance().SetOne<IRepository>(mockRepo);
    
    // Act - your code calls Create<IRepository>() and gets mockRepo
    var result = myService.ProcessData();
    
    // Assert
    Assert.Equal(expected, result);
    
    // Cleanup
    ObjectFactory.Instance().ClearAll();
}
```

#### Breaking Dependencies

Transform hard dependencies into testable code:

```csharp
// Legacy code with hard dependency
class UserService 
{
    public void ProcessUser(int id) 
    {
        var repo = new SqlRepository("server=prod;...");
        var user = repo.GetUser(id);
        // ...
    }
}

// Testable code using ObjectFactory
using static SpecRec.GlobalObjectFactory;

class UserService 
{
    public void ProcessUser(int id) 
    {
        var repo = Create<IRepository, SqlRepository>("server=prod;...");
        var user = repo.GetUser(id);
        // ...
    }
}
```

### CallLogger: Recording Interactions

**Use Case:** You need to understand what your legacy code actually does - what it calls, with what parameters, and what it expects back.

**Solution:** CallLogger records all method calls to create human-readable specifications.

#### With Context API

```csharp
[Theory]
[SpecRecLogs]
public async Task RecordInteractions(Context ctx)
{
    await ctx.Verify(async () =>
    {
        // Wraps services to log all calls automatically
        ctx.Wrap<IEmailService>(realEmailService, "📧");
        
        // Run your code - all calls are logged
        var result = await ProcessEmails();
        return result;
    });
}
```

#### In Regular Tests

```csharp
[Fact]
public async Task RecordManually()
{
    var logger = new CallLogger();
    var wrapped = logger.Wrap<IEmailService>(emailService, "📧");
    
    // Use wrapped service
    wrapped.SendEmail("user@example.com", "Hello");
    
    // Verify the log
    await Verify(logger.SpecBook.ToString());
}
```

#### Specification Format

CallLogger produces readable specifications:

```
📧 SendEmail:
  🔸 to: "user@example.com"
  🔸 subject: "Hello"
  🔹 Returns: True

📧 GetPendingEmails:
  🔸 maxCount: 10
  🔹 Returns: ["email1", "email2"]
```

### Parrot: Replaying Interactions

**Use Case:** You have recorded interactions and now want to replay them as test doubles without manually setting up mocks.

**Solution:** Parrot reads verified files and automatically provides the right return values.

#### With Context API

```csharp
[Theory]
[SpecRecLogs]
public async Task ReplayWithParrot(Context ctx)
{
    await ctx.Verify(async () =>
    {
        // Automatically creates Parrots from verified file
        ctx.Substitute<IEmailService>("📧")
           .Substitute<IUserService>("👤");
        
        // Your code gets Parrots that replay from verified file
        var result = ProcessUserFlow();
        return result;
    });
}
```

#### In Regular Tests

```csharp
[Fact]
public async Task ManualParrot()
{
    var callLog = CallLog.FromVerifiedFile();
    var parrot = new Parrot(callLog);
    
    var emailService = parrot.Create<IEmailService>("📧");
    var userService = parrot.Create<IUserService>("👤");
    
    // Use parrots as test doubles
    var result = ProcessWithServices(emailService, userService);
    
    // Verify all expected calls were made
    await Verify(callLog.ToString());
}
```

### Object ID Tracking

**Use Case:** Your methods pass around complex objects that are hard to serialize in specifications.

**Solution:** Register objects with IDs to show clean references instead of verbose dumps.

#### With Context API

```csharp
[Theory]
[SpecRecLogs]
public async Task TrackObjects(Context ctx)
{
    await ctx.Verify(async () =>
    {
        var complexConfig = new DatabaseConfig { /* ... */ };
        
        // Register with an ID
        ctx.Register(complexConfig, "dbConfig");
        
        // When logged, shows as <id:dbConfig> instead of full dump
        ctx.Substitute<IDataService>("🗃️");
        
        var service = Create<IDataService>();
        service.Initialize(complexConfig);  // Logs as <id:dbConfig>
    });
}
```

#### In Regular Tests

```csharp
[Fact]
public void TrackManually()
{
    var factory = ObjectFactory.Instance();
    var config = new DatabaseConfig();
    
    // Register object with ID
    factory.Register(config, "myConfig");
    
    var logger = new CallLogger();
    var wrapped = logger.Wrap<IService>(service, "🔧");
    
    // Call logs show <id:myConfig> instead of serialized object
    wrapped.Process(config);
}
```

### SpecRecLogs Attribute: Data-Driven Testing

**Use Case:** You want to test multiple scenarios with the same setup but different data.

**Solution:** SpecRecLogs automatically discovers verified files and creates a test for each.

#### File Structure

For a test method `TestUserScenarios`, create multiple verified files:
- `TestClass.TestUserScenarios.AdminUser.verified.txt`
- `TestClass.TestUserScenarios.RegularUser.verified.txt`
- `TestClass.TestUserScenarios.GuestUser.verified.txt`

Each becomes a separate test case.

#### With Parameters

Tests can accept parameters from verified files:

```csharp
[Theory]
[SpecRecLogs]
public async Task TestWithData(Context ctx, string userName, bool isAdmin = false)
{
    await ctx.Verify(async () =>
    {
        ctx.Substitute<IUserService>("👤");
        
        var service = Create<IUserService>();
        var result = service.CreateUser(userName, isAdmin);
        return $"Created: {userName} (Admin: {isAdmin})";
    });
}
```

Verified file with parameters:

```
📋 <Test Inputs>
  🔸 userName: "alice"
  🔸 isAdmin: True

👤 CreateUser:
  🔸 name: "alice"
  🔸 isAdmin: True
  🔹 Returns: 123

Created: alice (Admin: True)
```

### Advanced Features

#### Controlling What Gets Logged

Hide sensitive data or control output:

```csharp
public class MyService : IMyService
{
    public void ProcessSecret(string public, string secret)
    {
        CallLogFormatterContext.IgnoreArgument(1);  // Hide secret parameter
        // ...
    }
    
    public string GetToken()
    {
        CallLogFormatterContext.IgnoreReturnValue();  // Hide return value
        return "secret-token";
    }
}
```

#### Manual Test Doubles with LoggedReturnValue

Use `CallLogFormatterContext.LoggedReturnValue<T>()` to access parsed return values from verified files within your manual stubs.

```csharp
public class ManualEmailServiceStub : IEmailService
{
    public bool SendEmail(string to, string subject)
    {
        // Your custom logic here
        Console.WriteLine($"Sending email to {to}: {subject}");
        
        // Return the value from verified specification file
        return CallLogFormatterContext.LoggedReturnValue<bool>();
    }
    
    public List<string> GetPendingEmails()
    {
        // Custom processing logic
        ProcessPendingQueue();
        
        // Return parsed value from specification, with fallback
        return CallLogFormatterContext.LoggedReturnValue<List<string>>() ?? new List<string>();
    }
}
```

Use with verified specification files:

```
📧 SendEmail:
  🔸 to: "user@example.com"
  🔸 subject: "Welcome!"
  🔹 Returns: True

📧 GetPendingEmails:
  🔹 Returns: ["email1@test.com", "email2@test.com"]
```

```csharp
[Theory]
[SpecRecLogs]
public async Task TestWithManualStub(Context ctx)
{
    await ctx.Verify(async () =>
    {
        // Register your manual stub instead of auto-generated parrot
        var customStub = new ManualEmailServiceStub();
        ctx.Substitute<IEmailService>("📧", customStub);
        
        var service = Create<IEmailService>();
        var result = service.SendEmail("user@example.com", "Welcome!");
        
        return result; // Returns True from verified file
    });
}
```

#### Constructor Parameter Tracking

Track how objects are constructed:

```csharp
public class EmailService : IEmailService, IConstructorCalledWith
{
    public void ConstructorCalledWith(ConstructorParameterInfo[] parameters)
    {
        // Access constructor parameters
        // parameters[0].Name, parameters[0].Value, etc.
    }
}
```

#### Type-Safe Value Parsing

SpecRec enforces strict formatting in verified files:

- **Strings**: Must use quotes: `"hello"`
- **Booleans**: Case-sensitive: `True` or `False`
- **DateTime**: Format `yyyy-MM-dd HH:mm:ss`: `2023-12-25 14:30:45`
- **Arrays**: No spaces: `[1,2,3]` or `["a","b","c"]`
- **Objects**: Use IDs: `<id:myObject>`
- **Null**: Lowercase: `null`

## Requirements

- .NET 9.0+
- C# 13+
- xUnit (for examples) - any test framework works
- Verify framework (for approval testing)

## License

PolyForm Noncommercial License 1.0.0