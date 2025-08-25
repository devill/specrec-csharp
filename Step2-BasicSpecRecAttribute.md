# Step 2: Create Basic SpecRecAttribute (Verify Discovery Works)

## Objective
Create a minimal `SpecRecAttribute` that works identically to `[Theory][SpecRecLogs]` but as a single attribute. This verifies that the xUnit discovery mechanism works before adding Context complexity.

## Context
The unified interface will use `[SpecRec]` instead of `[Theory][SpecRecLogs]`. Before implementing Context transformation, we need to ensure basic test discovery works correctly.

## Prerequisites
- Step 1 completed successfully (clean baseline, 216 tests passing)
- Understanding of xUnit `TheoryAttribute` and `IDataDiscoverer` patterns
- Familiarity with existing `SpecRecLogsAttribute` implementation

## Success Criteria
- [ ] `SpecRecAttribute` class created and compiles
- [ ] Basic `SpecRecDiscoverer` delegates to existing `SpecRecLogsDiscoverer`
- [ ] Simple test with `[SpecRec]` runs identically to `[Theory][SpecRecLogs]`
- [ ] Test creates `.received.txt` file when no verified file exists
- [ ] Test reads from `.verified.txt` file when present
- [ ] All existing tests still pass (no regressions)

## Implementation Steps

### 2.1 Create SpecRecAttribute Class

Create `SpecRec/SpecRecAttribute.cs`:
```csharp
using Xunit;
using Xunit.Sdk;

namespace SpecRec
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [DataDiscoverer("SpecRec.SpecRecDiscoverer", "SpecRec")]
    public class SpecRecAttribute : TheoryAttribute
    {
        // Minimal implementation - xUnit will call the discoverer
    }
}
```

### 2.2 Create Basic SpecRecDiscoverer

Create `SpecRec/SpecRecDiscoverer.cs`:
```csharp
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SpecRec
{
    public class SpecRecDiscoverer : IDataDiscoverer
    {
        private readonly SpecRecLogsDiscoverer _baseDiscoverer;

        public SpecRecDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _baseDiscoverer = new SpecRecLogsDiscoverer(diagnosticMessageSink);
        }

        public IEnumerable<object[]> GetData(IAttributeInfo dataAttribute, IMethodInfo testMethod)
        {
            // For now, just delegate to existing discoverer - no Context transformation yet
            return _baseDiscoverer.GetData(dataAttribute, testMethod);
        }

        public bool SupportsDiscoveryEnumeration(IAttributeInfo dataAttribute, IMethodInfo testMethod)
        {
            return _baseDiscoverer.SupportsDiscoveryEnumeration(dataAttribute, testMethod);
        }
    }
}
```

### 2.3 Create Test to Verify Basic Functionality

Create `SpecRec.Tests/BasicSpecRecTests.cs`:
```csharp
using Xunit;

namespace SpecRec.Tests
{
    public interface IBasicTestService
    {
        string ProcessData(string input);
        int Calculate(int a, int b);
    }

    public class BasicSpecRecTests : IDisposable
    {
        public BasicSpecRecTests()
        {
            ObjectFactory.Instance().ClearAll();
        }

        public void Dispose()
        {
            ObjectFactory.Instance().ClearAll();
        }

        // This should work identically to [Theory][SpecRecLogs]
#pragma warning disable xUnit1003 // Theory methods must have test data - SpecRec provides data via discoverer
        [SpecRec]
        public async Task BasicCalculation(CallLog callLog, int valueA = 5, int valueB = 3)
        {
            // Use existing SpecRec patterns
            var calculator = Parrot.Create<IBasicTestService>(callLog, "🧮");
            
            var result = calculator.Calculate(valueA, valueB);
            callLog.AppendLine($"Result: {result}");
            
            await callLog.Verify();
        }
#pragma warning restore xUnit1003
    }
}
```

### 2.4 Test Basic Functionality

#### 2.4.1 Compile and Initial Run
```bash
# Ensure it compiles
dotnet build

# Run the test (should fail initially with missing return values)
dotnet test --filter "BasicCalculation" --verbosity normal
```

**Expected Behavior:**
- Test should run and create a `.received.txt` file
- Test should fail with `ParrotMissingReturnValueException` 
- File should be: `BasicSpecRecTests.BasicCalculation.FirstTestCase.received.txt`

#### 2.4.2 Create Verified File
Copy the `.received.txt` to `.verified.txt` and fill in return values:

`BasicSpecRecTests.BasicCalculation.FirstTestCase.verified.txt`:
```
🧮 Calculate:
  🔸 a: 5
  🔸 b: 3
  🔹 Returns: 8

Result: 8
```

#### 2.4.3 Verify Test Passes
```bash
# Test should now pass
dotnet test --filter "BasicCalculation" --verbosity normal
```

#### 2.4.4 Test Multiple Cases
Create additional verified files to test multiple cases:

`BasicSpecRecTests.BasicCalculation.LargeNumbers.verified.txt`:
```
📋 <Test Inputs>
  🔸 valueA: 100
  🔸 valueB: 50

🧮 Calculate:
  🔸 a: 100
  🔸 b: 50
  🔹 Returns: 150

Result: 150
```

Run tests again:
```bash
dotnet test --filter "BasicCalculation" --verbosity normal
```

**Expected:** Should see multiple test cases running:
- `BasicCalculation(callLog: FirstTestCase, valueA: 5, valueB: 3)`
- `BasicCalculation(callLog: LargeNumbers, valueA: 100, valueB: 50)`

### 2.5 Verify No Regressions
```bash
# Ensure all existing tests still pass
dotnet test --verbosity minimal

# Should still show 216+ tests passing (original + new tests)
```

## Verification Checklist
- [ ] `SpecRecAttribute` class compiles without errors
- [ ] `SpecRecDiscoverer` class compiles without errors  
- [ ] Test with `[SpecRec]` creates `.received.txt` file when no verified file exists
- [ ] Test passes when `.verified.txt` file has correct return values
- [ ] Multiple test cases work (FirstTestCase + custom cases)
- [ ] All existing SpecRec tests still pass
- [ ] No compilation errors or warnings

## Common Issues & Solutions

**Issue**: "No data found for test method"
**Solution**: Check discoverer registration in `SpecRecAttribute` DataDiscoverer attribute

**Issue**: Test doesn't create `.received.txt` file
**Solution**: Verify `SpecRecLogsDiscoverer` is being called correctly - add debugging

**Issue**: Can't find verified files
**Solution**: Ensure file naming matches pattern: `{Class}.{Method}.{TestCase}.verified.txt`

**Issue**: Compilation errors in discoverer
**Solution**: Ensure all using statements are correct, especially `Xunit.Abstractions`

## Debugging Tips

### Add Diagnostic Messages
```csharp
public IEnumerable<object[]> GetData(IAttributeInfo dataAttribute, IMethodInfo testMethod)
{
    var diagnosticMessage = $"SpecRecDiscoverer called for {testMethod.Type.Name}.{testMethod.Name}";
    _diagnosticMessageSink.OnMessage(new DiagnosticMessage(diagnosticMessage));
    
    var data = _baseDiscoverer.GetData(dataAttribute, testMethod);
    var count = data.Count();
    
    _diagnosticMessageSink.OnMessage(new DiagnosticMessage($"Found {count} test cases"));
    
    return data;
}
```

### Check File Discovery
```bash
# List verified files to ensure they're found
ls -la SpecRec.Tests/*BasicCalculation*.txt
```

## Next Step
Once all verification criteria pass, proceed to [Step 3: Add Context Class](Step3-AddContextClass.md).

## Notes  
- This step proves the discovery mechanism works before adding Context complexity
- The test should behave identically to existing `[Theory][SpecRecLogs]` tests
- Do not proceed to Context implementation until this step is 100% working