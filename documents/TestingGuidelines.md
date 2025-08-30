# Testing Guidelines

Why do we write tests?
- Helps design the interface from the user's perspective
- Documents the expected behavior
- And yes... it also let's us know if we broke something

But high documentation value is just as if not more important than detecting regressions.

**GUIDING PRINCIPLE**: Tests should be as close to actual use cases as possible.

Since we are developing a testing framework, this often means writing tests against example code.

## Testing Patterns

Most of our tests should follow the current recommended interface of SpecRec. For example:

```csharp
[Theory]
[SpecRecLogs]
public async Task MixingWrappersWithParrots(Context ctx)
{
    await ctx.Verify(async () =>
    {
        ctx.Substitute<IBatchProcessor>("🔄")
           .Substitute<INotificationService>("🔔");

        ctx.SetOne(ctx.Wrap<Random>(new RandomStub(), "🎲"));
        
        Create<Random>().Next(0, 5);
        Create<INotificationService>().SendUrgentNotification("Urgent notification");
        Create<IBatchProcessor>().GetProcessedCount();
    });
}
```

Unless there is good reason **ALWAYS** prefer the test pattern above.

### Good reasons to not use the preferred syntax

These situations should be rare, but when you run into them, you can rely on the strategies outlined bellow.

#### Support of features outside Context.Verify

We want to continue support of our core features even without the Context.Verify syntax. For that reason some tests will look use some features directly:

```csharp
[Fact]
    public async Task ParrotWithObjectFactory_ShouldLogConstructorWhenCreated()
    {
        var callLog = CallLog.FromVerifiedFile();
        
        var parrotService = Parrot.Create<ITestConstructorService>(callLog, "🦜");
        ObjectFactory.Instance().SetOne<ITestConstructorService>(parrotService);
        
        var createdService = ObjectFactory.Instance().Create<ITestConstructorService>("connectionString", 42);
        
        var result = createdService.DoSomething("test");
        
        await Verify(callLog.ToString());
    }
}
```

#### Testing error cases

Then the test is expecting an exception we might not be able to rely on Verify to check our assertions. In those cases some or all of these strategies can be used:
- Inline verified context and classic assertions instead of Verify
- Expect exceptions that are not logged by Context.Verify or are expected to pass through Context.Verify

#### Testing assumptions not related to the call log

One example would be testing the returned values from a parrot. The verified file will be used to read the value, and it will get logged back into the received file, but if the test is about making sure that a certain value is returned, or an exception is thrown by the Parrot, you may still need to test that with a classic assertion.

## Test Organization

Tests should be grouped by feature and use case. 